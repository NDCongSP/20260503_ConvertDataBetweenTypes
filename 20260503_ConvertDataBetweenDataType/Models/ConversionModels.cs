namespace ConvertDataBetweenDataType.Models;

/// <summary>
/// Các kiểu dữ liệu được hỗ trợ chuyển đổi
/// </summary>
public enum DataType
{
    DEC,    // Decimal (thập phân) - cơ số 10
    HEX,    // Hexadecimal (thập lục phân) - cơ số 16
    BIN,    // Binary (nhị phân) - cơ số 2
    OCT,    // Octal (bát phân) - cơ số 8
    WORD,   // Word (16-bit unsigned integer, 0 - 65535)
    BYTE,   // Byte (8-bit unsigned integer, 0 - 255)
    ASCII   // ASCII text ↔ mã số
}

/// <summary>
/// Kết quả chuyển đổi dữ liệu kèm diễn giải
/// </summary>
public class ConversionResult
{
    /// <summary>Có thành công không</summary>
    public bool IsSuccess { get; set; }

    /// <summary>Giá trị kết quả (dạng chuỗi)</summary>
    public string ResultValue { get; set; } = string.Empty;

    /// <summary>Thông báo lỗi nếu thất bại</summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>Các bước diễn giải chi tiết cách chuyển đổi</summary>
    public List<ConversionStep> Steps { get; set; } = new();

    /// <summary>Kiểu dữ liệu nguồn</summary>
    public DataType FromType { get; set; }

    /// <summary>Kiểu dữ liệu đích</summary>
    public DataType ToType { get; set; }

    /// <summary>Giá trị đầu vào gốc</summary>
    public string InputValue { get; set; } = string.Empty;

    /// <summary>Giá trị số nguyên trung gian (dùng nội bộ)</summary>
    public long IntermediateValue { get; set; }
}

/// <summary>
/// Một bước trong quá trình diễn giải chuyển đổi
/// </summary>
public class ConversionStep
{
    /// <summary>Số thứ tự bước</summary>
    public int StepNumber { get; set; }

    /// <summary>Tiêu đề bước</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Mô tả chi tiết bước này làm gì</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Công thức / phép tính minh hoạ</summary>
    public string Formula { get; set; } = string.Empty;

    /// <summary>Kết quả của bước này</summary>
    public string StepResult { get; set; } = string.Empty;
}

/// <summary>
/// Thông tin mô tả từng kiểu dữ liệu
/// </summary>
public class DataTypeInfo
{
    public DataType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Example { get; set; } = string.Empty;
    public string Prefix { get; set; } = string.Empty;
    public string ValidChars { get; set; } = string.Empty;
}
