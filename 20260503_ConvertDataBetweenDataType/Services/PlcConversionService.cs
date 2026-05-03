using ConvertDataBetweenDataType.Models;
using System.Text;

namespace ConvertDataBetweenDataType.Services;

/// <summary>
/// Service chuyển đổi kiểu dữ liệu PLC/SCADA theo mô hình thanh ghi Modbus.
///
/// Hai hướng chuyển đổi:
///   Decode: Thanh ghi Word (raw) ──[Byte Order]──► Kiểu dữ liệu đích
///   Encode: Kiểu dữ liệu nguồn ──[Byte Order]──► Thanh ghi Word (raw)
///
/// Số thanh ghi cần thiết:
///   1 thanh ghi (16-bit): Bool, Short, Word, BCD
///   2 thanh ghi (32-bit): Long, DWord, Float, LBCD
///   4 thanh ghi (64-bit): LInt, LWord, Double
/// </summary>
public class PlcConversionService
{
    // ══════════════════════════════════════════
    //  METADATA
    // ══════════════════════════════════════════

    public List<PlcDataTypeInfo> GetDataTypeInfos() => new()
    {
        new() { Type=PlcDataType.Bool,   Name="Bool",   BitWidth=1,  NeedsByteOrder=false, Category="1-bit",
            Description="Boolean 1-bit. Chỉ 0 (False) hoặc 1 (True).",
            ValidChars="0, 1", Example="1" },

        new() { Type=PlcDataType.Short,  Name="Short",  BitWidth=16, IsSigned=true,  NeedsByteOrder=true, Category="16-bit",
            Description="Số nguyên có dấu 16-bit. [-32 768 … 32 767]",
            ValidChars="-32768 … 32767", Example="-1234" },

        new() { Type=PlcDataType.Word,   Name="Word",   BitWidth=16, NeedsByteOrder=true, Category="16-bit",
            Description="Số nguyên không dấu 16-bit. [0 … 65 535]",
            ValidChars="0 … 65535", Example="12333" },

        new() { Type=PlcDataType.BCD,    Name="BCD",    BitWidth=16, NeedsByteOrder=true, Category="16-bit",
            Description="BCD 16-bit. Mỗi nibble = 1 chữ số thập phân. [0 … 9 999]",
            ValidChars="0 … 9999", Example="1234" },

        new() { Type=PlcDataType.Long,   Name="Long",   BitWidth=32, IsSigned=true,  NeedsByteOrder=true, Category="32-bit",
            Description="Số nguyên có dấu 32-bit. [-2 147 483 648 … 2 147 483 647]",
            ValidChars="-2147483648 … 2147483647", Example="-80000" },

        new() { Type=PlcDataType.DWord,  Name="DWord",  BitWidth=32, NeedsByteOrder=true, Category="32-bit",
            Description="Số nguyên không dấu 32-bit. [0 … 4 294 967 295]",
            ValidChars="0 … 4294967295", Example="305419896" },

        new() { Type=PlcDataType.Float,  Name="Float",  BitWidth=32, IsFloat=true,   NeedsByteOrder=true, Category="32-bit",
            Description="Số thực IEEE 754 đơn (Single) 32-bit.",
            ValidChars="số thực", Example="3.14" },

        new() { Type=PlcDataType.LBCD,   Name="LBCD",   BitWidth=32, NeedsByteOrder=true, Category="32-bit",
            Description="BCD 32-bit. 8 nibble = 8 chữ số. [0 … 99 999 999]",
            ValidChars="0 … 99999999", Example="12345678" },

        new() { Type=PlcDataType.LInt,   Name="LInt",   BitWidth=64, IsSigned=true,  NeedsByteOrder=true, Category="64-bit",
            Description="Số nguyên có dấu 64-bit. [±9.2×10¹⁸]",
            ValidChars="int 64-bit", Example="-9000000000" },

        new() { Type=PlcDataType.LWord,  Name="LWord",  BitWidth=64, NeedsByteOrder=true, Category="64-bit",
            Description="Số nguyên không dấu 64-bit. [0 … 1.8×10¹⁹]",
            ValidChars="uint 64-bit", Example="9000000000" },

        new() { Type=PlcDataType.Double, Name="Double", BitWidth=64, IsFloat=true,   NeedsByteOrder=true, Category="64-bit",
            Description="Số thực IEEE 754 kép (Double) 64-bit.",
            ValidChars="số thực", Example="3.14159265358979" },

        new() { Type=PlcDataType.String, Name="String", BitWidth=0,  NeedsByteOrder=false, Category="Text",
            Description="Chuỗi ASCII. Mỗi ký tự = 1 byte, 2 ký tự/thanh ghi.",
            ValidChars="text", Example="Hello" },
    };

    public List<ByteOrderInfo> GetByteOrderInfos() => new()
    {
        new() { Order=PlcByteOrder.HBHW, ShortName="HBHW",
            Name="High Byte / High Word First",
            Description="Big Endian. Byte cao nhất (MSB) ở thanh ghi đầu tiên. Chuẩn Modbus TCP, Siemens S7." },
        new() { Order=PlcByteOrder.HBLW, ShortName="HBLW",
            Name="High Byte / Low Word First",
            Description="Word-Swap Big Endian. Word thấp ở thanh ghi đầu. Dùng trong một số PLC Schneider." },
        new() { Order=PlcByteOrder.LBHW, ShortName="LBHW",
            Name="Low Byte / High Word First",
            Description="Byte-Swap Little Endian. Word cao ở đầu nhưng byte đảo trong word. Allen-Bradley." },
        new() { Order=PlcByteOrder.LBLW, ShortName="LBLW",
            Name="Low Byte / Low Word First",
            Description="Little Endian. Byte thấp nhất (LSB) ở thanh ghi đầu tiên. Chuẩn x86, nhiều RTU." },
    };

    public PlcDataTypeInfo GetInfo(PlcDataType t) => GetDataTypeInfos().First(x => x.Type == t);
    public ByteOrderInfo GetByteOrderInfo(PlcByteOrder o) => GetByteOrderInfos().First(x => x.Order == o);

    /// <summary>Số thanh ghi Word (16-bit) cần thiết cho kiểu dữ liệu này</summary>
    public int GetRegisterCount(PlcDataType type) => type switch
    {
        PlcDataType.Bool or PlcDataType.Short or PlcDataType.Word or PlcDataType.BCD => 1,
        PlcDataType.Long or PlcDataType.DWord or PlcDataType.Float or PlcDataType.LBCD => 2,
        PlcDataType.LInt or PlcDataType.LWord or PlcDataType.Double => 4,
        PlcDataType.String => 1, // variable
        _ => 1
    };

    // ══════════════════════════════════════════
    //  VALIDATE
    // ══════════════════════════════════════════

    /// <summary>Validate một giá trị thanh ghi (hex 0x0000-0xFFFF hoặc decimal 0-65535)</summary>
    public (bool Ok, string Error) ValidateRegister(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return (false, "Không được để trống.");
        var s = input.Trim().ToUpper().Replace("0X", "").Replace(" ", "");
        if (System.Text.RegularExpressions.Regex.IsMatch(s, @"^[0-9A-F]{1,4}$"))
            return System.Convert.ToInt32(s, 16) <= 0xFFFF ? (true, "") : (false, "Vượt 0xFFFF.");
        return ushort.TryParse(input.Trim(), out _) ? (true, "") : (false, "HEX (0x0000-0xFFFF) hoặc DEC (0-65535).");
    }

    /// <summary>Validate giá trị theo kiểu dữ liệu đích</summary>
    public (bool Ok, string Error) Validate(string value, PlcDataType type)
    {
        if (string.IsNullOrWhiteSpace(value)) return (false, "Không được để trống.");
        value = value.Trim();
        return type switch
        {
            PlcDataType.Bool   => (value is "0" or "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase)
                                    || value.Equals("false", StringComparison.OrdinalIgnoreCase), "Bool: 0, 1, True, False."),
            PlcDataType.Short  => short.TryParse(value, out _) ? (true,"") : (false,"Short: -32768 đến 32767."),
            PlcDataType.Word   => ushort.TryParse(value, out _) ? (true,"") : (false,"Word: 0 đến 65535."),
            PlcDataType.BCD    => int.TryParse(value, out var b) && b >= 0 && b <= 9999 ? (true,"") : (false,"BCD: 0 đến 9999."),
            PlcDataType.Long   => int.TryParse(value, out _) ? (true,"") : (false,"Long: -2 147 483 648 đến 2 147 483 647."),
            PlcDataType.DWord  => uint.TryParse(value, out _) ? (true,"") : (false,"DWord: 0 đến 4 294 967 295."),
            PlcDataType.Float  => float.TryParse(value, System.Globalization.NumberStyles.Float,
                                    System.Globalization.CultureInfo.InvariantCulture, out _) ? (true,"") : (false,"Float: số thực (vd: 3.14)."),
            PlcDataType.LBCD   => long.TryParse(value, out var l) && l >= 0 && l <= 99_999_999 ? (true,"") : (false,"LBCD: 0 đến 99 999 999."),
            PlcDataType.LInt   => long.TryParse(value, out _) ? (true,"") : (false,"LInt: số nguyên 64-bit có dấu."),
            PlcDataType.LWord  => ulong.TryParse(value, out _) ? (true,"") : (false,"LWord: 0 đến 1.8×10¹⁹."),
            PlcDataType.Double => double.TryParse(value, System.Globalization.NumberStyles.Float,
                                    System.Globalization.CultureInfo.InvariantCulture, out _) ? (true,"") : (false,"Double: số thực 64-bit."),
            PlcDataType.String => value.Length > 0 ? (true,"") : (false,"String không rỗng."),
            _ => (false, "Kiểu không hỗ trợ.")
        };
    }

    // ══════════════════════════════════════════
    //  DECODE: Registers ──► Typed Value
    // ══════════════════════════════════════════

    public PlcConversionResult DecodeFromRegisters(List<string> regInputs, PlcDataType toType, PlcByteOrder order)
    {
        var result = new PlcConversionResult { FromType = PlcDataType.Word, ToType = toType };
        ushort[] regs = regInputs.Select(ParseRegInput).ToArray();

        // ── Xử lý riêng kiểu String ──────────────────────────────
        if (toType == PlcDataType.String)
            return DecodeStringFromRegisters(regs, order);

        int step = 1;
        result.InputValue = string.Join(" | ", regs.Select((r, i) => $"Reg[{i}]=0x{r:X4}"));

        // 1. Giải thích thanh ghi đầu vào
        var inputFormula = new StringBuilder();
        for (int i = 0; i < regs.Length; i++)
            inputFormula.AppendLine($"  Reg[{i}] (địa chỉ base+{i}) = 0x{regs[i]:X4} = {regs[i]}₁₀ = {Convert.ToString(regs[i], 2).PadLeft(16, '0')}₂");
        result.Steps.Add(Step(step++,
            $"Đọc {regs.Length} thanh ghi Word (16-bit) từ thiết bị",
            "Mỗi thanh ghi Modbus là 16-bit. Giá trị được đọc theo thứ tự địa chỉ tăng dần (Reg[0] = địa chỉ base).",
            inputFormula.ToString().TrimEnd(),
            $"Tổng raw: {regs.Length} × 16-bit = {regs.Length * 16}-bit"));

        // 2. Apply byte order → raw bits
        ulong rawBits = RegistersToULong(regs, order);
        int bitWidth = regs.Length * 16;
        result.RawHex = FormatHex(rawBits, bitWidth);

        // 3. Byte order explanation
        result.Steps.Add(BuildDecodeByteOrderStep(step++, regs, order, rawBits));

        // 4. Interpret bits as target type
        try
        {
            result.ResultValue = InterpretBitsAsType(rawBits, toType, result, ref step);
        }
        catch (Exception ex)
        {
            result.ErrorMessage = ex.Message;
            return result;
        }

        // 5. Register layout hiển thị lại đúng byte order
        result.Registers = ULongToRegisterRows(rawBits, bitWidth, order);
        result.IsSuccess = true;
        return result;
    }

    // ══════════════════════════════════════════
    //  ENCODE: Typed Value ──► Registers
    // ══════════════════════════════════════════

    public PlcConversionResult EncodeToRegisters(string value, PlcDataType fromType, PlcByteOrder order)
    {
        var result = new PlcConversionResult { FromType = fromType, ToType = PlcDataType.Word, InputValue = value };

        // ── Xử lý riêng kiểu String ──────────────────────────────
        if (fromType == PlcDataType.String)
            return EncodeStringToRegisters(value, order);

        int step = 1;

        var (ok, err) = Validate(value, fromType);
        if (!ok) { result.ErrorMessage = err; return result; }

        int numRegs = GetRegisterCount(fromType);
        int bitWidth = numRegs * 16;
        ulong rawBits = 0;

        try
        {
            // Parse typed value → raw bits
            rawBits = ParseTypedToRawBits(value, fromType, result, ref step);
            result.RawHex = FormatHex(rawBits, bitWidth);

            // Encode raw bits → register array
            ushort[] regs = ULongToRegisters(rawBits, numRegs, order);

            // Show byte order step
            result.Steps.Add(BuildEncodeByteOrderStep(step++, rawBits, bitWidth, order, regs));

            // Build result string + register rows
            result.ResultValue = string.Join("  |  ", regs.Select((r, i) => $"Reg[{i}]=0x{r:X4}"));
            result.Registers = BuildRegisterRows(regs, order, rawBits, bitWidth);
        }
        catch (Exception ex)
        {
            result.ErrorMessage = ex.Message;
            return result;
        }

        result.IsSuccess = true;
        return result;
    }

    // ══════════════════════════════════════════
    //  CORE: RegistersToULong / ULongToRegisters
    // ══════════════════════════════════════════

    /// <summary>
    /// Ghép các thanh ghi thành giá trị nguyên theo byte order.
    /// Ví dụ HBHW: [Reg0=0x1234, Reg1=0x5678] → 0x12345678
    /// </summary>
    public static ulong RegistersToULong(ushort[] regs, PlcByteOrder order)
    {
        bool swapWords = order is PlcByteOrder.HBLW or PlcByteOrder.LBLW;
        bool swapBytes = order is PlcByteOrder.LBHW or PlcByteOrder.LBLW;

        ushort[] ws = (ushort[])regs.Clone();
        if (swapWords) Array.Reverse(ws); // đảo thứ tự word → high word về đầu

        byte[] bytes = new byte[regs.Length * 2];
        for (int i = 0; i < ws.Length; i++)
        {
            // swapBytes: trong thanh ghi, byte thấp mang bit cao hơn
            bytes[i * 2]     = swapBytes ? (byte)(ws[i] & 0xFF) : (byte)(ws[i] >> 8);
            bytes[i * 2 + 1] = swapBytes ? (byte)(ws[i] >> 8)  : (byte)(ws[i] & 0xFF);
        }

        ulong v = 0;
        foreach (var b in bytes) v = (v << 8) | b;
        return v;
    }

    /// <summary>
    /// Tách giá trị nguyên thành các thanh ghi theo byte order.
    /// Ví dụ HBHW: 0x12345678 → [Reg0=0x1234, Reg1=0x5678]
    /// </summary>
    public static ushort[] ULongToRegisters(ulong value, int numRegs, PlcByteOrder order)
    {
        bool swapWords = order is PlcByteOrder.HBLW or PlcByteOrder.LBLW;
        bool swapBytes = order is PlcByteOrder.LBHW or PlcByteOrder.LBLW;

        int n = numRegs * 2;
        byte[] bytes = new byte[n];
        for (int i = 0; i < n; i++)
            bytes[i] = (byte)((value >> ((n - 1 - i) * 8)) & 0xFF);

        ushort[] regs = new ushort[numRegs];
        for (int i = 0; i < numRegs; i++)
        {
            regs[i] = swapBytes
                ? (ushort)((bytes[i * 2 + 1] << 8) | bytes[i * 2])   // đảo byte trong word
                : (ushort)((bytes[i * 2] << 8) | bytes[i * 2 + 1]);  // giữ nguyên
        }

        if (swapWords) Array.Reverse(regs); // đảo thứ tự word
        return regs;
    }

    // ══════════════════════════════════════════
    //  INTERPRET raw bits → typed string
    // ══════════════════════════════════════════

    private string InterpretBitsAsType(ulong rawBits, PlcDataType toType, PlcConversionResult result, ref int step)
    {
        switch (toType)
        {
            case PlcDataType.Bool:
            {
                ushort boolWord = (ushort)(rawBits & 0xFFFF);
                string hiBin = Convert.ToString((boolWord >> 8) & 0xFF, 2).PadLeft(8, '0');
                string loBin = Convert.ToString(boolWord & 0xFF, 2).PadLeft(8, '0');

                // Đếm bit bật
                var setBits = new List<string>();
                for (int b = 15; b >= 0; b--)
                    if (((boolWord >> b) & 1) == 1)
                        setBits.Add($"Bit{b}");

                var formula = new StringBuilder();
                formula.AppendLine($"  Giá trị Word: 0x{boolWord:X4} = {boolWord}₁₀");
                formula.AppendLine();
                formula.AppendLine($"  High Byte [Bit 15–8]: {hiBin}");
                formula.AppendLine($"              Bit:       15 14 13 12 11 10  9  8");
                for (int b = 15; b >= 8; b--)
                    formula.Append($"               Bit{b:D2} = {(boolWord >> b) & 1}  ");
                formula.AppendLine();
                formula.AppendLine();
                formula.AppendLine($"  Low  Byte [Bit  7–0]: {loBin}");
                formula.AppendLine($"              Bit:        7  6  5  4  3  2  1  0");
                formula.AppendLine($"  Các bit = 1: {(setBits.Count > 0 ? string.Join(", ", setBits) : "Không có")}");

                result.Steps.Add(Step(step++,
                    "Giải mã Bool – Hiển thị đầy đủ 16 bit (chia 2 nhóm 8-bit)",
                    "Word 16-bit được biểu diễn thành 16 giá trị Bool riêng lẻ. " +
                    "High Byte = Bit[15..8], Low Byte = Bit[7..0]. Mỗi bit: 1 = True, 0 = False.",
                    formula.ToString().TrimEnd(),
                    $"High: {hiBin}  |  Low: {loBin}  ({setBits.Count}/16 bit = 1)"));

                // Lưu thêm metadata vào result để UI render bit-grid
                result.BoolHighByte = hiBin;
                result.BoolLowByte  = loBin;
                result.BoolWord     = boolWord;

                return $"{hiBin} {loBin}";
            }

            case PlcDataType.Short:
                short sv = (short)(ushort)(rawBits & 0xFFFF);
                result.Steps.Add(Step(step++, "Giải mã Short (16-bit có dấu – Two's Complement)",
                    "Nếu bit 15 = 1 thì số âm. Công thức: giá trị = rawBits - 65536 (khi rawBits ≥ 32768).",
                    BuildSignedDecode((ushort)(rawBits & 0xFFFF), 16),
                    $"= {sv}"));
                return sv.ToString();

            case PlcDataType.Word:
                ushort wv = (ushort)(rawBits & 0xFFFF);
                result.Steps.Add(Step(step++, "Giải mã Word (16-bit không dấu)",
                    "Word là số nguyên không dấu 16-bit. Giá trị = các bit ghép lại theo hệ thập phân.",
                    $"0x{wv:X4} = {Convert.ToString(wv, 2).PadLeft(16, '0')}₂ = {wv}₁₀",
                    $"{wv}"));
                return wv.ToString();

            case PlcDataType.BCD:
                ushort bcdRaw = (ushort)(rawBits & 0xFFFF);
                if (!IsBCDValid(bcdRaw, 4)) throw new Exception($"Dữ liệu 0x{bcdRaw:X4} không hợp lệ BCD (nibble > 9).");
                long bcdVal = DecBCD(bcdRaw, 4);
                result.Steps.Add(Step(step++, "Giải mã BCD 16-bit",
                    "Mỗi nibble (4 bit) là 1 chữ số thập phân (0-9). BCD 16-bit = 4 chữ số.",
                    BuildBCDDecodeFormula(bcdRaw, 4), $"= {bcdVal}"));
                return bcdVal.ToString();

            case PlcDataType.Long:
                int lv = (int)(uint)(rawBits & 0xFFFFFFFF);
                result.Steps.Add(Step(step++, "Giải mã Long (32-bit có dấu – Two's Complement)",
                    "Nếu bit 31 = 1 thì số âm. Giá trị = rawBits - 4294967296 (khi rawBits ≥ 2147483648).",
                    BuildSignedDecode((uint)(rawBits & 0xFFFFFFFF), 32),
                    $"= {lv}"));
                return lv.ToString();

            case PlcDataType.DWord:
                uint dv = (uint)(rawBits & 0xFFFFFFFF);
                result.Steps.Add(Step(step++, "Giải mã DWord (32-bit không dấu)",
                    "DWord = Double Word. Số nguyên không dấu 32-bit. Phạm vi 0 đến 4 294 967 295.",
                    $"0x{dv:X8} = {dv}₁₀",
                    $"{dv}"));
                return dv.ToString();

            case PlcDataType.Float:
                float fv = BitConverter.UInt32BitsToSingle((uint)(rawBits & 0xFFFFFFFF));
                string fStr = FormatFloat(fv);
                result.Steps.Add(Step(step++, "Giải mã Float (IEEE 754 Single 32-bit)",
                    "32-bit raw được phân tích thành: 1 bit dấu | 8 bit số mũ | 23 bit định trị (mantissa).",
                    BuildIEEE754Single((uint)(rawBits & 0xFFFFFFFF)),
                    fStr));
                return fStr;

            case PlcDataType.LBCD:
                uint lbcdRaw = (uint)(rawBits & 0xFFFFFFFF);
                if (!IsBCDValid(lbcdRaw, 8)) throw new Exception($"Dữ liệu 0x{lbcdRaw:X8} không hợp lệ LBCD.");
                long lbcdVal = DecBCD(lbcdRaw, 8);
                result.Steps.Add(Step(step++, "Giải mã LBCD 32-bit",
                    "8 nibble, mỗi nibble là 1 chữ số thập phân. LBCD 32-bit = 8 chữ số.",
                    BuildBCDDecodeFormula(lbcdRaw, 8), $"= {lbcdVal}"));
                return lbcdVal.ToString();

            case PlcDataType.LInt:
                long liv = (long)rawBits;
                result.Steps.Add(Step(step++, "Giải mã LInt (64-bit có dấu)",
                    "Nếu bit 63 = 1 thì số âm (Two's Complement 64-bit).",
                    BuildSignedDecode(rawBits, 64), $"= {liv}"));
                return liv.ToString();

            case PlcDataType.LWord:
                result.Steps.Add(Step(step++, "Giải mã LWord (64-bit không dấu)",
                    "Số nguyên không dấu 64-bit. Phạm vi 0 đến 18 446 744 073 709 551 615.",
                    $"0x{rawBits:X16} = {rawBits}₁₀", rawBits.ToString()));
                return rawBits.ToString();

            case PlcDataType.Double:
                double dbl = BitConverter.UInt64BitsToDouble(rawBits);
                string dblStr = FormatDouble(dbl);
                result.Steps.Add(Step(step++, "Giải mã Double (IEEE 754 Double 64-bit)",
                    "64-bit raw: 1 bit dấu | 11 bit số mũ | 52 bit định trị.",
                    BuildIEEE754Double(rawBits),
                    dblStr));
                return dblStr;

            default:
                return rawBits.ToString();
        }
    }

    // ══════════════════════════════════════════
    //  PARSE typed string → raw bits (ulong)
    // ══════════════════════════════════════════

    private ulong ParseTypedToRawBits(string value, PlcDataType type, PlcConversionResult result, ref int step)
    {
        ulong bits = 0;
        switch (type)
        {
            case PlcDataType.Bool:
                bits = (value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase)) ? 1UL : 0UL;
                result.Steps.Add(Step(step++, "Chuyển Bool sang raw bits",
                    "Bool chiếm 1 bit trong thanh ghi (bit 0). Giá trị được lưu vào 1 thanh ghi Word.",
                    $"{value} → bit0 = {bits}", $"0x{bits:X4}"));
                break;

            case PlcDataType.Short:
                short s = short.Parse(value);
                bits = (ushort)s;
                result.Steps.Add(Step(step++, "Chuyển Short sang raw bits 16-bit (Two's Complement)",
                    s >= 0
                        ? $"Số dương: biểu diễn trực tiếp thành 16-bit."
                        : $"Số âm: bù 2 = NOT({(ushort)~s:X4}) + 1 = {bits:X4}.",
                    $"{value}₁₀ → 0x{bits:X4} = {Convert.ToString((long)bits, 2).PadLeft(16, '0')}₂",
                    $"0x{bits:X4}"));
                break;

            case PlcDataType.Word:
                bits = ushort.Parse(value);
                result.Steps.Add(Step(step++, "Chuyển Word sang raw bits",
                    "Word không dấu: biểu diễn trực tiếp thành 16-bit.",
                    $"{value}₁₀ = 0x{bits:X4} = {Convert.ToString((long)bits, 2).PadLeft(16, '0')}₂",
                    $"0x{bits:X4}"));
                break;

            case PlcDataType.BCD:
                int bcdDec = int.Parse(value);
                bits = (ulong)EncBCD(bcdDec, 4);
                result.Steps.Add(Step(step++, "Mã hoá BCD 16-bit",
                    "Mỗi chữ số thập phân → 1 nibble (4 bit). BCD 16-bit chứa 4 chữ số.",
                    BuildBCDEncodeFormula(bcdDec, 4),
                    $"BCD = 0x{bits:X4}"));
                break;

            case PlcDataType.Long:
                int l = int.Parse(value);
                bits = (uint)l;
                result.Steps.Add(Step(step++, "Chuyển Long sang raw bits 32-bit (Two's Complement)",
                    l >= 0 ? "Số dương: biểu diễn trực tiếp."
                           : $"Số âm: bù 2 = NOT({(uint)~l:X8}) + 1 = {bits:X8}.",
                    $"{value}₁₀ → 0x{bits:X8}",
                    $"0x{bits:X8}"));
                break;

            case PlcDataType.DWord:
                bits = uint.Parse(value);
                result.Steps.Add(Step(step++, "Chuyển DWord sang raw bits 32-bit",
                    "DWord không dấu 32-bit.",
                    $"{value}₁₀ = 0x{bits:X8}",
                    $"0x{bits:X8}"));
                break;

            case PlcDataType.Float:
                float f = float.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
                bits = BitConverter.SingleToUInt32Bits(f);
                result.Steps.Add(Step(step++, "Mã hoá Float (IEEE 754 Single 32-bit)",
                    "IEEE 754 gồm: 1 bit dấu | 8 bit số mũ bias-127 | 23 bit định trị.",
                    BuildIEEE754Single((uint)bits),
                    $"Raw bits = 0x{bits:X8}"));
                break;

            case PlcDataType.LBCD:
                long lbcdDec = long.Parse(value);
                bits = (ulong)EncBCD(lbcdDec, 8);
                result.Steps.Add(Step(step++, "Mã hoá LBCD 32-bit",
                    "8 chữ số thập phân → 8 nibble (32-bit).",
                    BuildBCDEncodeFormula(lbcdDec, 8),
                    $"LBCD = 0x{bits:X8}"));
                break;

            case PlcDataType.LInt:
                long li = long.Parse(value);
                bits = (ulong)li;
                result.Steps.Add(Step(step++, "Chuyển LInt sang raw bits 64-bit",
                    li >= 0 ? "Số dương 64-bit." : "Số âm 64-bit: bù 2.",
                    $"{value}₁₀ → 0x{bits:X16}",
                    $"0x{bits:X16}"));
                break;

            case PlcDataType.LWord:
                bits = ulong.Parse(value);
                result.Steps.Add(Step(step++, "Chuyển LWord sang raw bits 64-bit",
                    "LWord không dấu 64-bit.",
                    $"{value}₁₀ = 0x{bits:X16}",
                    $"0x{bits:X16}"));
                break;

            case PlcDataType.Double:
                double d = double.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
                bits = BitConverter.DoubleToUInt64Bits(d);
                result.Steps.Add(Step(step++, "Mã hoá Double (IEEE 754 Double 64-bit)",
                    "IEEE 754 Double: 1 bit dấu | 11 bit số mũ bias-1023 | 52 bit định trị.",
                    BuildIEEE754Double(bits),
                    $"Raw bits = 0x{bits:X16}"));
                break;
        }
        return bits;
    }

    // ══════════════════════════════════════════
    //  REGISTER LAYOUT BUILDERS
    // ══════════════════════════════════════════

    private List<RegisterRow> ULongToRegisterRows(ulong rawBits, int bitWidth, PlcByteOrder order)
    {
        int numRegs = bitWidth / 16;
        ushort[] regs = ULongToRegisters(rawBits, numRegs, order);
        return BuildRegisterRows(regs, order, rawBits, bitWidth);
    }

    private List<RegisterRow> BuildRegisterRows(ushort[] regs, PlcByteOrder order, ulong rawBits, int bitWidth)
    {
        bool swapBytes = order is PlcByteOrder.LBHW or PlcByteOrder.LBLW;
        return regs.Select((r, i) => new RegisterRow
        {
            RegisterIndex = i,
            HighByte = (r >> 8).ToString("X2"),
            LowByte  = (r & 0xFF).ToString("X2"),
            RegisterHex = r.ToString("X4"),
            RegisterDec = r.ToString(),
            Note = i == 0 ? "Thanh ghi 1 (địa chỉ base)"
                          : $"Thanh ghi {i + 1} (địa chỉ base+{i})"
        }).ToList();
    }

    private PlcConversionStep BuildDecodeByteOrderStep(int n, ushort[] regs, PlcByteOrder order, ulong result)
    {
        bool swapW = order is PlcByteOrder.HBLW or PlcByteOrder.LBLW;
        bool swapB = order is PlcByteOrder.LBHW or PlcByteOrder.LBLW;
        var boInfo = GetByteOrderInfo(order);

        var sb = new StringBuilder();
        sb.AppendLine($"  Byte Order: {boInfo.Name} ({order})");
        sb.AppendLine($"  → {(swapW ? "Đảo thứ tự Word (Low Word First)" : "Giữ thứ tự Word (High Word First)")}");
        sb.AppendLine($"  → {(swapB ? "Đảo byte trong mỗi Word (Low Byte First)" : "Giữ byte trong Word (High Byte First)")}");
        sb.AppendLine();

        ushort[] ws = (ushort[])regs.Clone();
        if (swapW) { Array.Reverse(ws); sb.AppendLine($"  Sau đảo Word: [{string.Join(", ", ws.Select(r => $"0x{r:X4}"))}]"); }

        sb.AppendLine($"  Ghép bytes (Big-Endian):");
        int bitWidth = regs.Length * 16;
        sb.AppendLine($"  → Raw = {FormatHex(result, bitWidth)} = {result}₁₀");

        return Step(n, $"Áp dụng Byte Order [{order}] để ghép thanh ghi → raw bits",
            boInfo.Description, sb.ToString().TrimEnd(),
            FormatHex(result, bitWidth));
    }

    private PlcConversionStep BuildEncodeByteOrderStep(int n, ulong rawBits, int bitWidth, PlcByteOrder order, ushort[] regs)
    {
        bool swapW = order is PlcByteOrder.HBLW or PlcByteOrder.LBLW;
        bool swapB = order is PlcByteOrder.LBHW or PlcByteOrder.LBLW;
        var boInfo = GetByteOrderInfo(order);

        var sb = new StringBuilder();
        sb.AppendLine($"  Raw bits: {FormatHex(rawBits, bitWidth)}");
        sb.AppendLine($"  Byte Order: {boInfo.Name}");
        sb.AppendLine($"  → {(swapB ? "Đảo byte trong mỗi Word" : "Giữ nguyên byte trong Word")}");
        sb.AppendLine($"  → {(swapW ? "Đảo thứ tự Word" : "Giữ thứ tự Word")}");
        sb.AppendLine();
        for (int i = 0; i < regs.Length; i++)
            sb.AppendLine($"  Reg[{i}] = 0x{regs[i]:X4} = {regs[i]}₁₀  ({(i == 0 ? "địa chỉ base" : $"base+{i}")})");

        return Step(n, $"Tách raw bits → {regs.Length} thanh ghi theo Byte Order [{order}]",
            boInfo.Description, sb.ToString().TrimEnd(),
            string.Join(" | ", regs.Select((r, i) => $"Reg[{i}]=0x{r:X4}")));
    }

    // ══════════════════════════════════════════
    //  BCD
    // ══════════════════════════════════════════

    private static long EncBCD(long dec, int nibbles)
    {
        long r = 0;
        for (int i = 0; i < nibbles; i++) { r |= (dec % 10) << (i * 4); dec /= 10; }
        return r;
    }

    private static long DecBCD(ulong bcd, int nibbles)
    {
        long r = 0, m = 1;
        for (int i = 0; i < nibbles; i++) { r += (long)((bcd >> (i * 4)) & 0xF) * m; m *= 10; }
        return r;
    }

    private static bool IsBCDValid(ulong bcd, int nibbles)
    {
        for (int i = 0; i < nibbles; i++) if (((bcd >> (i * 4)) & 0xF) > 9) return false;
        return true;
    }

    private static string BuildBCDEncodeFormula(long dec, int nibbles)
    {
        string digits = dec.ToString().PadLeft(nibbles, '0');
        var sb = new StringBuilder();
        sb.AppendLine($"  Số thập phân: {dec} → {nibbles} chữ số: {digits}");
        for (int i = 0; i < digits.Length; i++)
            sb.AppendLine($"    Chữ số [{i}] '{digits[i]}' → nibble {nibbles - 1 - i} = 0x{digits[i]}");
        sb.Append($"  BCD hex = 0x{string.Join("", digits)}");
        return sb.ToString();
    }

    private static string BuildBCDDecodeFormula(ulong bcd, int nibbles)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"  BCD raw = 0x{bcd:X}");
        long total = 0, mult = 1;
        for (int i = 0; i < nibbles; i++)
        {
            long d = (long)((bcd >> (i * 4)) & 0xF);
            sb.AppendLine($"    Nibble[{i}] = {d} × {mult} = {d * mult}");
            total += d * mult; mult *= 10;
        }
        sb.Append($"  Tổng = {total}₁₀");
        return sb.ToString();
    }

    // ══════════════════════════════════════════
    //  SIGNED DECODE EXPLANATION
    // ══════════════════════════════════════════

    private static string BuildSignedDecode(ulong raw, int bits)
    {
        ulong signBit = 1UL << (bits - 1);
        bool neg = (raw & signBit) != 0;
        if (!neg) return $"  Bit {bits - 1} = 0 → số dương. Giá trị = {raw}₁₀";

        ulong mask = bits < 64 ? (1UL << bits) - 1 : ulong.MaxValue;
        ulong mag = (~raw + 1) & mask;
        return $"  Bit {bits - 1} = 1 → số âm (Two's Complement)\n" +
               $"  Đảo bit: {(~raw & mask):X} → Cộng 1 → {mag:X}\n" +
               $"  Giá trị = -{mag}";
    }

    // ══════════════════════════════════════════
    //  IEEE 754 EXPLANATIONS
    // ══════════════════════════════════════════

    private static string BuildIEEE754Single(uint bits)
    {
        int sign = (int)(bits >> 31);
        int exp  = (int)((bits >> 23) & 0xFF);
        uint mant = bits & 0x7FFFFF;
        double val = BitConverter.UInt32BitsToSingle(bits);
        return $"  Dấu   [bit 31]    : {sign} → {(sign == 0 ? "Dương (+)" : "Âm (-)")}\n" +
               $"  Số mũ [bit 30-23] : {exp} → bias-127 → 2^({exp}-127) = 2^{exp - 127}\n" +
               $"  Định trị [22-0]   : 0x{mant:X6} → 1.{(mant / 8388608.0):F10}\n" +
               $"  Giá trị = {(sign == 0 ? "+" : "-")} × 2^{exp - 127} × (1 + {mant / 8388608.0:F8})\n" +
               $"          ≈ {val:G7}";
    }

    private static string BuildIEEE754Double(ulong bits)
    {
        int sign  = (int)(bits >> 63);
        int exp   = (int)((bits >> 52) & 0x7FF);
        ulong mant = bits & 0x000FFFFFFFFFFFFFUL;
        double val = BitConverter.UInt64BitsToDouble(bits);
        return $"  Dấu   [bit 63]     : {sign} → {(sign == 0 ? "Dương (+)" : "Âm (-)")}\n" +
               $"  Số mũ [bit 62-52]  : {exp} → bias-1023 → 2^({exp}-1023)\n" +
               $"  Định trị [bit 51-0]: 0x{mant:X13}\n" +
               $"  Giá trị ≈ {val:G17}";
    }

    // ══════════════════════════════════════════
    //  STRING ENCODE / DECODE (đặc biệt — không dùng rawBits ulong)
    // ══════════════════════════════════════════

    /// <summary>Số thanh ghi cần thiết để lưu chuỗi s (2 ký tự/thanh ghi, tối thiểu 1)</summary>
    public static int GetStringRegisterCount(string s) =>
        string.IsNullOrEmpty(s) ? 1 : Math.Max(1, (s.Length + 1) / 2);

    /// <summary>Encode chuỗi ASCII → thanh ghi Word, 2 ký tự/thanh ghi</summary>
    private PlcConversionResult EncodeStringToRegisters(string value, PlcByteOrder order)
    {
        bool swapBytes = order is PlcByteOrder.LBHW or PlcByteOrder.LBLW;
        var result = new PlcConversionResult { FromType = PlcDataType.String, ToType = PlcDataType.Word, InputValue = value };
        int step = 1;

        // Chuyển sang byte ASCII
        byte[] ascii = Encoding.ASCII.GetBytes(value);
        if (ascii.Length % 2 != 0) ascii = [.. ascii, 0]; // padding '\0' nếu lẻ
        int numRegs = ascii.Length / 2;

        // Bước 1 — ASCII lookup
        var asciiSb = new StringBuilder();
        asciiSb.AppendLine($"  Chuỗi: \"{value}\" ({value.Length} ký tự → {numRegs} thanh ghi)");
        asciiSb.AppendLine($"  (Mỗi thanh ghi Word chứa 2 ký tự; nếu số ký tự lẻ → thêm byte 0x00)");
        asciiSb.AppendLine();
        for (int i = 0; i < ascii.Length; i++)
            asciiSb.AppendLine($"  Byte[{i}] = 0x{ascii[i]:X2} = {ascii[i],3} = " +
                               (ascii[i] >= 32 ? $"'{(char)ascii[i]}'" : "PAD(\\0)"));

        result.Steps.Add(Step(step++, "Chuyển từng ký tự sang mã ASCII (0-127)",
            "Mỗi ký tự ASCII chiếm 1 byte (8 bit). 2 byte ghép thành 1 thanh ghi Word 16-bit.",
            asciiSb.ToString().TrimEnd(),
            $"{value.Length} ký tự → {ascii.Length} byte → {numRegs} thanh ghi"));

        // Bước 2 — đóng gói vào thanh ghi
        ushort[] regs = new ushort[numRegs];
        var packSb = new StringBuilder();
        packSb.AppendLine($"  Byte Order: [{order}] → {(swapBytes ? "Low Byte = ký tự đầu, High Byte = ký tự sau" : "High Byte = ký tự đầu, Low Byte = ký tự sau")}");
        packSb.AppendLine();

        for (int i = 0; i < numRegs; i++)
        {
            byte b1 = ascii[i * 2];       // ký tự thứ nhất của cặp
            byte b2 = ascii[i * 2 + 1];   // ký tự thứ hai (hoặc 0x00 padding)
            regs[i] = swapBytes
                ? (ushort)((b2 << 8) | b1)   // LO = ký tự đầu
                : (ushort)((b1 << 8) | b2);  // HI = ký tự đầu
            string c1 = b1 >= 32 ? $"'{(char)b1}'" : "PAD";
            string c2 = b2 >= 32 ? $"'{(char)b2}'" : "PAD";
            if (swapBytes)
                packSb.AppendLine($"  Reg[{i}]: LoByte={c1}(0x{b1:X2}) | HiByte={c2}(0x{b2:X2}) → 0x{regs[i]:X4} = {regs[i]}");
            else
                packSb.AppendLine($"  Reg[{i}]: HiByte={c1}(0x{b1:X2}) | LoByte={c2}(0x{b2:X2}) → 0x{regs[i]:X4} = {regs[i]}");
        }

        result.Steps.Add(Step(step++, $"Đóng gói 2 ký tự/thanh ghi theo Byte Order [{order}]",
            "High Byte = ký tự đầu, Low Byte = ký tự tiếp theo (HBHW/HBLW). " +
            "Low Byte = ký tự đầu, High Byte = ký tự tiếp theo (LBHW/LBLW).",
            packSb.ToString().TrimEnd(),
            string.Join(" | ", regs.Select((r, i) => $"Reg[{i}]=0x{r:X4}"))));

        result.ResultValue  = string.Join("  |  ", regs.Select((r, i) => $"Reg[{i}]=0x{r:X4} ({r})"));
        result.RawHex       = string.Join(" ", regs.Select(r => $"0x{r:X4}"));
        result.Registers    = BuildStringRegisterRows(regs, swapBytes, ascii);
        result.IsSuccess    = true;
        return result;
    }

    /// <summary>Decode thanh ghi Word → chuỗi ASCII, 2 ký tự/thanh ghi</summary>
    private PlcConversionResult DecodeStringFromRegisters(ushort[] regs, PlcByteOrder order)
    {
        bool swapBytes = order is PlcByteOrder.LBHW or PlcByteOrder.LBLW;
        var result = new PlcConversionResult { FromType = PlcDataType.Word, ToType = PlcDataType.String };
        int step = 1;

        result.InputValue = string.Join(" | ", regs.Select((r, i) => $"Reg[{i}]=0x{r:X4}"));

        // Bước 1 — hiển thị thanh ghi
        var inputSb = new StringBuilder();
        for (int i = 0; i < regs.Length; i++)
            inputSb.AppendLine($"  Reg[{i}] = 0x{regs[i]:X4} = {regs[i]}₁₀ = {Convert.ToString(regs[i], 2).PadLeft(16, '0')}₂");

        result.Steps.Add(Step(step++, $"Đọc {regs.Length} thanh ghi Word từ thiết bị",
            "Mỗi thanh ghi Word 16-bit chứa 2 ký tự ASCII (High Byte = ký tự 1, Low Byte = ký tự 2, với HBHW).",
            inputSb.ToString().TrimEnd(),
            $"{regs.Length} thanh ghi = {regs.Length * 2} ký tự tối đa"));

        // Bước 2 — tách bytes
        var decodeSb  = new StringBuilder();
        var charList  = new List<char>();
        decodeSb.AppendLine($"  Byte Order [{order}]: {(swapBytes ? "Low Byte = ký tự đầu" : "High Byte = ký tự đầu")}");
        decodeSb.AppendLine();

        byte[] rawAscii = new byte[regs.Length * 2];
        for (int i = 0; i < regs.Length; i++)
        {
            byte hi = (byte)(regs[i] >> 8);
            byte lo = (byte)(regs[i] & 0xFF);
            byte char1 = swapBytes ? lo : hi;
            byte char2 = swapBytes ? hi : lo;
            rawAscii[i * 2]     = char1;
            rawAscii[i * 2 + 1] = char2;

            string c1 = char1 >= 32 ? $"'{(char)char1}'" : (char1 == 0 ? "NUL" : $"0x{char1:X2}");
            string c2 = char2 >= 32 ? $"'{(char)char2}'" : (char2 == 0 ? "NUL" : $"0x{char2:X2}");
            if (swapBytes)
                decodeSb.AppendLine($"  Reg[{i}]=0x{regs[i]:X4}: LoByte=0x{char1:X2}={c1}(ký tự {i*2+1}) | HiByte=0x{char2:X2}={c2}(ký tự {i*2+2})");
            else
                decodeSb.AppendLine($"  Reg[{i}]=0x{regs[i]:X4}: HiByte=0x{char1:X2}={c1}(ký tự {i*2+1}) | LoByte=0x{char2:X2}={c2}(ký tự {i*2+2})");

            if (char1 != 0) charList.Add((char)char1);
            if (char2 != 0) charList.Add((char)char2);
        }

        result.Steps.Add(Step(step++, $"Tách bytes theo Byte Order [{order}]",
            "Mỗi thanh ghi Word → 2 byte ASCII. Dừng ở byte NUL (0x00) nếu có.",
            decodeSb.ToString().TrimEnd(),
            $"{charList.Count} ký tự tìm thấy"));

        // Bước 3 — ghép chuỗi
        string str = new(charList.ToArray());
        var charFormula = new StringBuilder();
        charFormula.AppendLine($"  Ghép {charList.Count} ký tự theo thứ tự:");
        for (int i = 0; i < charList.Count; i++)
            charFormula.Append($"  [{i}]='{charList[i]}'(0x{(int)charList[i]:X2})");
        charFormula.AppendLine();
        charFormula.Append($"  → Chuỗi: \"{str}\"");

        result.Steps.Add(Step(step++, "Ghép chuỗi ASCII hoàn chỉnh",
            "Các ký tự ASCII được nối lại theo đúng thứ tự. Byte NUL (0x00) = kết thúc chuỗi.",
            charFormula.ToString().TrimEnd(),
            $"\"{str}\""));

        result.ResultValue = str;
        result.RawHex      = string.Join(" ", regs.Select(r => $"0x{r:X4}"));
        result.Registers   = BuildStringRegisterRows(regs, swapBytes, rawAscii);
        result.IsSuccess   = true;
        return result;
    }

    private static List<RegisterRow> BuildStringRegisterRows(ushort[] regs, bool swapBytes, byte[] ascii)
    {
        var rows = new List<RegisterRow>();
        for (int i = 0; i < regs.Length; i++)
        {
            byte b1 = i * 2     < ascii.Length ? ascii[i * 2]     : (byte)0;
            byte b2 = i * 2 + 1 < ascii.Length ? ascii[i * 2 + 1] : (byte)0;
            string c1 = b1 >= 32 ? ((char)b1).ToString() : "\\0";
            string c2 = b2 >= 32 ? ((char)b2).ToString() : "\\0";
            rows.Add(new RegisterRow
            {
                RegisterIndex = i,
                HighByte      = (regs[i] >> 8).ToString("X2"),
                LowByte       = (regs[i] & 0xFF).ToString("X2"),
                RegisterHex   = regs[i].ToString("X4"),
                RegisterDec   = regs[i].ToString(),
                Note          = swapBytes
                    ? $"LoByte='{c1}'(0x{b1:X2}) · HiByte='{c2}'(0x{b2:X2})"
                    : $"HiByte='{c1}'(0x{b1:X2}) · LoByte='{c2}'(0x{b2:X2})"
            });
        }
        return rows;
    }

    // ══════════════════════════════════════════
    //  HELPERS
    // ══════════════════════════════════════════

    /// <summary>
    /// Định dạng Float kiểu SCADA: tránh ký hiệu khoa học cho các giá trị thông thường.
    /// Ví dụ: -123.34 thay vì -1.2334E+02; 0.001234 thay vì 1.234E-03.
    /// </summary>
    private static string FormatFloat(float f)
    {
        if (float.IsNaN(f))              return "NaN";
        if (float.IsPositiveInfinity(f)) return "+Infinity";
        if (float.IsNegativeInfinity(f)) return "-Infinity";
        float abs = Math.Abs(f);
        // Phạm vi "bình thường" trong SCADA/PLC: dùng định dạng thập phân cố định
        if (abs == 0f || (abs >= 0.001f && abs < 1_000_000_000f))
            return f.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture);
        // Số quá lớn hoặc quá nhỏ (sub-normal) → giữ G nhưng thêm chú thích
        return f.ToString("G7", System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Định dạng Double kiểu SCADA: tránh ký hiệu khoa học cho các giá trị thông thường.
    /// </summary>
    private static string FormatDouble(double d)
    {
        if (double.IsNaN(d))              return "NaN";
        if (double.IsPositiveInfinity(d)) return "+Infinity";
        if (double.IsNegativeInfinity(d)) return "-Infinity";
        double abs = Math.Abs(d);
        if (abs == 0d || (abs >= 0.000001 && abs < 1_000_000_000_000d))
            return d.ToString("0.##########", System.Globalization.CultureInfo.InvariantCulture);
        return d.ToString("G17", System.Globalization.CultureInfo.InvariantCulture);
    }

    private static ushort ParseRegInput(string input)
    {
        var s = input.Trim().ToUpper().Replace("0X", "").Replace(" ", "");
        if (s.Any(c => c >= 'A' && c <= 'F') || (s.Length == 4 && s.All(c => char.IsAsciiHexDigit(c))))
            return (ushort)System.Convert.ToInt32(s, 16);
        if (ushort.TryParse(s, out var dec)) return dec;
        return (ushort)System.Convert.ToInt32(s, 16);
    }

    private static string FormatHex(ulong v, int bitWidth) => bitWidth switch
    {
        16 => $"0x{v & 0xFFFF:X4}",
        32 => $"0x{v & 0xFFFFFFFF:X8}",
        64 => $"0x{v:X16}",
        _  => $"0x{v:X}"
    };

    private static PlcConversionStep Step(int n, string title, string desc, string formula, string res) =>
        new() { StepNumber = n, Title = title, Description = desc, Formula = formula, StepResult = res };
}
