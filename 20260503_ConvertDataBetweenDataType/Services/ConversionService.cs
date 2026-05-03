using ConvertDataBetweenDataType.Models;
using System.Text;

namespace ConvertDataBetweenDataType.Services;

/// <summary>
/// Service xử lý toàn bộ logic chuyển đổi dữ liệu giữa các kiểu số
/// Hỗ trợ: DEC ↔ HEX ↔ BIN ↔ OCT ↔ WORD ↔ BYTE ↔ ASCII
/// </summary>
public class ConversionService
{
    // ─────────────────────────────────────────────
    //  Thông tin mô tả các kiểu dữ liệu
    // ─────────────────────────────────────────────
    public List<DataTypeInfo> GetDataTypeInfos() => new()
    {
        new() { Type = DataType.DEC,   Name = "DEC (Decimal)",          Prefix = "",   ValidChars = "0-9",        Description = "Số thập phân, cơ số 10. Dùng các chữ số 0–9.",                        Example = "255" },
        new() { Type = DataType.HEX,   Name = "HEX (Hexadecimal)",      Prefix = "0x", ValidChars = "0-9, A-F",   Description = "Số thập lục phân, cơ số 16. Dùng 0–9 và A–F.",                         Example = "FF" },
        new() { Type = DataType.BIN,   Name = "BIN (Binary)",           Prefix = "0b", ValidChars = "0, 1",       Description = "Số nhị phân, cơ số 2. Chỉ dùng 0 và 1.",                               Example = "11111111" },
        new() { Type = DataType.OCT,   Name = "OCT (Octal)",            Prefix = "0o", ValidChars = "0-7",        Description = "Số bát phân, cơ số 8. Dùng các chữ số 0–7.",                           Example = "377" },
        new() { Type = DataType.WORD,  Name = "WORD (16-bit)",          Prefix = "",   ValidChars = "0-65535",    Description = "Số nguyên 16-bit không dấu. Phạm vi: 0 đến 65535 (2¹⁶ − 1).",          Example = "12333" },
        new() { Type = DataType.BYTE,  Name = "BYTE (8-bit)",           Prefix = "",   ValidChars = "0-255",      Description = "Số nguyên 8-bit không dấu. Phạm vi: 0 đến 255 (2⁸ − 1).",             Example = "255" },
        new() { Type = DataType.ASCII, Name = "ASCII (Text/Character)",  Prefix = "",   ValidChars = "text",       Description = "Ký tự hoặc chuỗi ASCII. Mỗi ký tự tương ứng một mã số (0–127).",      Example = "A" },
    };

    public DataTypeInfo GetInfo(DataType type) =>
        GetDataTypeInfos().First(x => x.Type == type);

    // ─────────────────────────────────────────────
    //  VALIDATE đầu vào
    // ─────────────────────────────────────────────
    public (bool IsValid, string Error) Validate(string value, DataType type)
    {
        if (string.IsNullOrWhiteSpace(value))
            return (false, "Giá trị không được để trống.");

        value = value.Trim().ToUpperInvariant();

        switch (type)
        {
            case DataType.DEC:
                if (!System.Text.RegularExpressions.Regex.IsMatch(value, @"^\d+$"))
                    return (false, "DEC chỉ chứa chữ số 0–9.");
                if (!long.TryParse(value, out var dec) || dec < 0)
                    return (false, "Giá trị DEC không hợp lệ.");
                return (true, "");

            case DataType.HEX:
                var hexVal = value.Replace("0X", "").Replace(" ", "");
                if (!System.Text.RegularExpressions.Regex.IsMatch(hexVal, @"^[0-9A-F]+$"))
                    return (false, "HEX chỉ chứa ký tự 0–9 và A–F.");
                return (true, "");

            case DataType.BIN:
                var binVal = value.Replace("0B", "").Replace(" ", "");
                if (!System.Text.RegularExpressions.Regex.IsMatch(binVal, @"^[01]+$"))
                    return (false, "BIN chỉ chứa 0 và 1.");
                return (true, "");

            case DataType.OCT:
                var octVal = value.Replace("0O", "").Replace(" ", "");
                if (!System.Text.RegularExpressions.Regex.IsMatch(octVal, @"^[0-7]+$"))
                    return (false, "OCT chỉ chứa chữ số 0–7.");
                return (true, "");

            case DataType.WORD:
                if (!long.TryParse(value, out var word) || word < 0 || word > 65535)
                    return (false, "WORD phải là số nguyên từ 0 đến 65535.");
                return (true, "");

            case DataType.BYTE:
                if (!long.TryParse(value, out var b) || b < 0 || b > 255)
                    return (false, "BYTE phải là số nguyên từ 0 đến 255.");
                return (true, "");

            case DataType.ASCII:
                if (value.Length == 0)
                    return (false, "ASCII không được để trống.");
                return (true, "");

            default:
                return (false, "Kiểu dữ liệu không được hỗ trợ.");
        }
    }

    // ─────────────────────────────────────────────
    //  CHUYỂN ĐỔI CHÍNH
    // ─────────────────────────────────────────────
    public ConversionResult Convert(string inputValue, DataType fromType, DataType toType)
    {
        var result = new ConversionResult
        {
            FromType = fromType,
            ToType = toType,
            InputValue = inputValue
        };

        // 1. Validate
        var (isValid, error) = Validate(inputValue, fromType);
        if (!isValid)
        {
            result.IsSuccess = false;
            result.ErrorMessage = error;
            return result;
        }

        // 2. Nếu from == to, trả về ngay
        if (fromType == toType)
        {
            result.IsSuccess = true;
            result.ResultValue = inputValue;
            result.Steps.Add(new ConversionStep
            {
                StepNumber = 1,
                Title = "Kiểu nguồn và đích giống nhau",
                Description = "Không cần chuyển đổi, giá trị giữ nguyên.",
                StepResult = inputValue
            });
            return result;
        }

        try
        {
            // 3. Bước 1: Parse về số nguyên trung gian (long)
            var (intermediate, parseSteps) = ParseToLong(inputValue.Trim(), fromType);
            result.IntermediateValue = intermediate;
            result.Steps.AddRange(parseSteps);

            // 4. Bước 2: Format sang kiểu đích
            var (output, formatSteps) = FormatFromLong(intermediate, toType, fromType, inputValue);
            result.ResultValue = output;
            result.Steps.AddRange(formatSteps);

            result.IsSuccess = true;
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.ErrorMessage = $"Lỗi chuyển đổi: {ex.Message}";
        }

        return result;
    }

    // ─────────────────────────────────────────────
    //  PARSE → LONG (bước trung gian)
    // ─────────────────────────────────────────────
    private (long Value, List<ConversionStep> Steps) ParseToLong(string input, DataType fromType)
    {
        var steps = new List<ConversionStep>();
        long value;

        switch (fromType)
        {
            case DataType.DEC:
                value = long.Parse(input);
                steps.Add(new ConversionStep
                {
                    StepNumber = 1,
                    Title = "Đọc số Decimal (DEC)",
                    Description = $"Giá trị '{input}' là số thập phân cơ số 10. " +
                                  $"Đây là dạng số thông thường chúng ta dùng hàng ngày.",
                    Formula = $"{input}₁₀ = {value}",
                    StepResult = value.ToString()
                });
                break;

            case DataType.HEX:
                var hexClean = input.ToUpper().Replace("0X", "").Replace(" ", "");
                value = System.Convert.ToInt64(hexClean, 16);
                var hexExpanded = BuildHexExpansion(hexClean);
                steps.Add(new ConversionStep
                {
                    StepNumber = 1,
                    Title = "Đọc số Hexadecimal (HEX) → Decimal",
                    Description = $"HEX dùng cơ số 16. Mỗi chữ số HEX nhân với 16 lũy thừa vị trí từ phải sang trái (bắt đầu từ 0).",
                    Formula = hexExpanded,
                    StepResult = $"= {value}₁₀"
                });
                break;

            case DataType.BIN:
                var binClean = input.Replace("0B", "").Replace("0b", "").Replace(" ", "");
                value = System.Convert.ToInt64(binClean, 2);
                var binExpanded = BuildBinExpansion(binClean);
                steps.Add(new ConversionStep
                {
                    StepNumber = 1,
                    Title = "Đọc số Binary (BIN) → Decimal",
                    Description = $"BIN dùng cơ số 2. Mỗi bit nhân với 2 lũy thừa vị trí từ phải sang trái (bắt đầu từ 0).",
                    Formula = binExpanded,
                    StepResult = $"= {value}₁₀"
                });
                break;

            case DataType.OCT:
                var octClean = input.Replace("0O", "").Replace("0o", "").Replace(" ", "");
                value = System.Convert.ToInt64(octClean, 8);
                var octExpanded = BuildOctExpansion(octClean);
                steps.Add(new ConversionStep
                {
                    StepNumber = 1,
                    Title = "Đọc số Octal (OCT) → Decimal",
                    Description = $"OCT dùng cơ số 8. Mỗi chữ số nhân với 8 lũy thừa vị trí từ phải sang trái (bắt đầu từ 0).",
                    Formula = octExpanded,
                    StepResult = $"= {value}₁₀"
                });
                break;

            case DataType.WORD:
                value = long.Parse(input);
                steps.Add(new ConversionStep
                {
                    StepNumber = 1,
                    Title = "Đọc giá trị WORD (16-bit)",
                    Description = $"WORD là số nguyên không dấu 16-bit, phạm vi 0–65535 (= 2¹⁶ − 1). " +
                                  $"Giá trị '{input}' hợp lệ và được lưu dưới dạng 16-bit nhị phân.",
                    Formula = $"{input} = {System.Convert.ToString(value, 2).PadLeft(16, '0')}₂ (16 bits)",
                    StepResult = value.ToString()
                });
                break;

            case DataType.BYTE:
                value = long.Parse(input);
                steps.Add(new ConversionStep
                {
                    StepNumber = 1,
                    Title = "Đọc giá trị BYTE (8-bit)",
                    Description = $"BYTE là số nguyên không dấu 8-bit, phạm vi 0–255 (= 2⁸ − 1). " +
                                  $"Giá trị '{input}' hợp lệ và được lưu dưới dạng 8-bit nhị phân.",
                    Formula = $"{input} = {System.Convert.ToString(value, 2).PadLeft(8, '0')}₂ (8 bits)",
                    StepResult = value.ToString()
                });
                break;

            case DataType.ASCII:
                // Lấy mã ASCII của ký tự đầu tiên (hoặc tổng nếu nhiều ký tự)
                value = input[0];
                var asciiDesc = input.Length == 1
                    ? $"Ký tự '{input}' có mã ASCII = {value}."
                    : $"Chỉ lấy ký tự đầu tiên '{input[0]}' có mã ASCII = {value}. Bảng ASCII chuẩn gồm 128 ký tự (0–127).";
                steps.Add(new ConversionStep
                {
                    StepNumber = 1,
                    Title = "Đọc mã ASCII",
                    Description = asciiDesc,
                    Formula = $"'{input[0]}' → ASCII code = {value}",
                    StepResult = value.ToString()
                });
                break;

            default:
                throw new NotSupportedException($"Kiểu '{fromType}' chưa được hỗ trợ.");
        }

        return (value, steps);
    }

    // ─────────────────────────────────────────────
    //  FORMAT LONG → KIỂU ĐÍCH
    // ─────────────────────────────────────────────
    private (string Output, List<ConversionStep> Steps) FormatFromLong(
        long value, DataType toType, DataType fromType, string originalInput)
    {
        var steps = new List<ConversionStep>();
        string output;

        switch (toType)
        {
            case DataType.DEC:
                output = value.ToString();
                steps.Add(new ConversionStep
                {
                    StepNumber = 2,
                    Title = $"Chuyển sang Decimal (DEC)",
                    Description = $"Decimal là cơ số 10. Giá trị số nguyên {value} biểu diễn thẳng sang DEC.",
                    Formula = $"Giá trị thập phân = {value}",
                    StepResult = output
                });
                break;

            case DataType.HEX:
                output = value.ToString("X");
                var hexSteps = BuildDivisionSteps(value, 16, "HEX");
                steps.Add(new ConversionStep
                {
                    StepNumber = 2,
                    Title = $"Chuyển {value}₁₀ sang Hexadecimal (HEX)",
                    Description = "Chia liên tiếp cho 16, lấy các số dư theo thứ tự ngược lại. " +
                                  "Số dư 10=A, 11=B, 12=C, 13=D, 14=E, 15=F.",
                    Formula = hexSteps,
                    StepResult = $"= {output}₁₆"
                });
                break;

            case DataType.BIN:
                output = System.Convert.ToString(value, 2);
                var binSteps = BuildDivisionSteps(value, 2, "BIN");
                steps.Add(new ConversionStep
                {
                    StepNumber = 2,
                    Title = $"Chuyển {value}₁₀ sang Binary (BIN)",
                    Description = "Chia liên tiếp cho 2, lấy các số dư theo thứ tự ngược lại. " +
                                  "Số dư chỉ là 0 hoặc 1.",
                    Formula = binSteps,
                    StepResult = $"= {output}₂"
                });
                // Thêm bước nhóm bit cho dễ đọc
                if (output.Length > 4)
                {
                    var grouped = GroupBits(output);
                    steps.Add(new ConversionStep
                    {
                        StepNumber = 3,
                        Title = "Nhóm bit để dễ đọc",
                        Description = "Nhóm các bit thành nhóm 4 (nibble) từ phải sang trái để dễ đọc hơn. " +
                                      "Mỗi nhóm 4 bit tương ứng 1 chữ số HEX.",
                        Formula = $"{output} → {grouped}",
                        StepResult = grouped
                    });
                }
                break;

            case DataType.OCT:
                output = System.Convert.ToString(value, 8);
                var octSteps = BuildDivisionSteps(value, 8, "OCT");
                steps.Add(new ConversionStep
                {
                    StepNumber = 2,
                    Title = $"Chuyển {value}₁₀ sang Octal (OCT)",
                    Description = "Chia liên tiếp cho 8, lấy các số dư theo thứ tự ngược lại.",
                    Formula = octSteps,
                    StepResult = $"= {output}₈"
                });
                break;

            case DataType.WORD:
                if (value < 0 || value > 65535)
                    throw new OverflowException($"Giá trị {value} vượt quá phạm vi WORD (0–65535).");
                output = value.ToString();
                var wordBin = System.Convert.ToString(value, 2).PadLeft(16, '0');
                var wordGrouped = GroupBitsFixed(wordBin, 4);
                steps.Add(new ConversionStep
                {
                    StepNumber = 2,
                    Title = $"Chuyển sang WORD (16-bit unsigned)",
                    Description = $"WORD lưu trữ 16-bit. Giá trị {value} được biểu diễn bằng 16 bit nhị phân, " +
                                  "chia thành 4 nhóm (mỗi nhóm 4 bit = 1 nibble = 1 chữ số HEX).",
                    Formula = $"{value}₁₀ = {wordGrouped}₂ (16-bit)\n" +
                              $"High Byte: {wordBin[..8]} = {System.Convert.ToInt64(wordBin[..8], 2)}₁₀\n" +
                              $"Low  Byte: {wordBin[8..]} = {System.Convert.ToInt64(wordBin[8..], 2)}₁₀",
                    StepResult = $"{output} (WORD) = {wordGrouped}₂"
                });
                break;

            case DataType.BYTE:
                if (value < 0 || value > 255)
                    throw new OverflowException($"Giá trị {value} vượt quá phạm vi BYTE (0–255).");
                output = value.ToString();
                var byteBin = System.Convert.ToString(value, 2).PadLeft(8, '0');
                steps.Add(new ConversionStep
                {
                    StepNumber = 2,
                    Title = $"Chuyển sang BYTE (8-bit unsigned)",
                    Description = $"BYTE lưu trữ 8-bit. Giá trị {value} được biểu diễn bằng 8 bit nhị phân.",
                    Formula = $"{value}₁₀ = {byteBin}₂ (8-bit)\n" +
                              $"HEX: {value:X2}  |  Bit 7 (MSB): {byteBin[0]}  |  Bit 0 (LSB): {byteBin[7]}",
                    StepResult = $"{output} (BYTE) = {byteBin}₂"
                });
                break;

            case DataType.ASCII:
                if (value < 0 || value > 127)
                    throw new OverflowException($"Giá trị {value} không có ký tự ASCII tương ứng (chỉ 0–127).");
                output = ((char)value).ToString();
                steps.Add(new ConversionStep
                {
                    StepNumber = 2,
                    Title = "Chuyển sang ký tự ASCII",
                    Description = $"Bảng ASCII chuẩn ánh xạ số nguyên 0–127 tới ký tự tương ứng. " +
                                  $"Mã {value} tương ứng với ký tự '{output}'.",
                    Formula = $"ASCII[{value}] = '{output}'",
                    StepResult = output
                });
                break;

            default:
                throw new NotSupportedException($"Kiểu đích '{toType}' chưa được hỗ trợ.");
        }

        return (output, steps);
    }

    // ─────────────────────────────────────────────
    //  HELPER: SINH DIỄN GIẢI MỞ RỘNG
    // ─────────────────────────────────────────────

    private string BuildHexExpansion(string hex)
    {
        hex = hex.ToUpper();
        var parts = new List<string>();
        for (int i = 0; i < hex.Length; i++)
        {
            int pos = hex.Length - 1 - i;
            int digitVal = "0123456789ABCDEF".IndexOf(hex[i]);
            int exp = hex.Length - 1 - i;
            parts.Add($"{hex[i]}({digitVal}) × 16^{exp}");
        }
        long total = System.Convert.ToInt64(hex, 16);
        return string.Join(" + ", parts) + $" = {total}";
    }

    private string BuildBinExpansion(string bin)
    {
        var parts = new List<string>();
        for (int i = 0; i < bin.Length; i++)
        {
            int bit = bin[i] - '0';
            int exp = bin.Length - 1 - i;
            if (bit == 1)
                parts.Add($"1 × 2^{exp}");
        }
        long total = System.Convert.ToInt64(bin, 2);
        return (parts.Count > 0 ? string.Join(" + ", parts) : "0") + $" = {total}";
    }

    private string BuildOctExpansion(string oct)
    {
        var parts = new List<string>();
        for (int i = 0; i < oct.Length; i++)
        {
            int d = oct[i] - '0';
            int exp = oct.Length - 1 - i;
            parts.Add($"{d} × 8^{exp}");
        }
        long total = System.Convert.ToInt64(oct, 8);
        return string.Join(" + ", parts) + $" = {total}";
    }

    private string BuildDivisionSteps(long value, int baseNum, string typeName)
    {
        if (value == 0) return $"0 ÷ {baseNum} = 0 dư 0  →  {typeName} = 0";

        var sb = new StringBuilder();
        var remainders = new List<string>();
        long n = value;
        int step = 1;

        while (n > 0)
        {
            long quotient = n / baseNum;
            long remainder = n % baseNum;
            string remStr = baseNum == 16 ? HexDigit(remainder) : remainder.ToString();
            sb.AppendLine($"  Bước {step++}: {n} ÷ {baseNum} = {quotient}  dư  {remStr}");
            remainders.Add(remStr);
            n = quotient;
        }

        remainders.Reverse();
        sb.Append($"  Đọc dư từ dưới lên: {string.Join("", remainders)}");
        return sb.ToString();
    }

    private static string HexDigit(long v) => v switch
    {
        10 => "A", 11 => "B", 12 => "C", 13 => "D", 14 => "E", 15 => "F", _ => v.ToString()
    };

    private static string GroupBits(string bits)
    {
        // Pad trái để chia hết thành nhóm 4
        int pad = (4 - bits.Length % 4) % 4;
        bits = new string('0', pad) + bits;
        var groups = new List<string>();
        for (int i = 0; i < bits.Length; i += 4)
            groups.Add(bits.Substring(i, 4));
        return string.Join(" ", groups);
    }

    private static string GroupBitsFixed(string bits, int groupSize)
    {
        var groups = new List<string>();
        for (int i = 0; i < bits.Length; i += groupSize)
            groups.Add(bits.Substring(i, Math.Min(groupSize, bits.Length - i)));
        return string.Join(" ", groups);
    }

    // ─────────────────────────────────────────────
    //  Bảng chuyển đổi tổng quan (cho tất cả kiểu)
    // ─────────────────────────────────────────────
    public List<AllTypesRow> ConvertToAllTypes(string input, DataType fromType)
    {
        var result = new List<AllTypesRow>();
        var (isValid, _) = Validate(input, fromType);
        if (!isValid) return result;

        var (intermediate, _) = ParseToLong(input.Trim(), fromType);

        foreach (DataType dt in Enum.GetValues<DataType>())
        {
            try
            {
                var (output, _) = FormatFromLong(intermediate, dt, fromType, input);
                result.Add(new AllTypesRow
                {
                    TypeName = dt.ToString(),
                    Value = output,
                    IsSource = dt == fromType
                });
            }
            catch
            {
                result.Add(new AllTypesRow
                {
                    TypeName = dt.ToString(),
                    Value = "— (vượt phạm vi)",
                    IsSource = dt == fromType
                });
            }
        }

        return result;
    }
}

public class AllTypesRow
{
    public string TypeName { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool IsSource { get; set; }
}
