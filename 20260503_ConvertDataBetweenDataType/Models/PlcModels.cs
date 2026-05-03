namespace ConvertDataBetweenDataType.Models;

/// <summary>
/// Kiểu dữ liệu PLC/SCADA (Kepware, WinCC, Ignition, Modbus...)
/// </summary>
public enum PlcDataType
{
    // ── 1-bit ──────────────────────────────────────────────────
    Bool,

    // ── 16-bit ─────────────────────────────────────────────────
    Short,   // Signed   16-bit  [-32 768 … 32 767]
    Word,    // Unsigned 16-bit  [0 … 65 535]
    BCD,     // 16-bit Binary Coded Decimal  [0 … 9 999]

    // ── 32-bit ─────────────────────────────────────────────────
    Long,    // Signed   32-bit  [-2 147 483 648 … 2 147 483 647]
    DWord,   // Unsigned 32-bit  [0 … 4 294 967 295]
    Float,   // IEEE 754 Single precision 32-bit
    LBCD,    // 32-bit Binary Coded Decimal  [0 … 99 999 999]

    // ── 64-bit ─────────────────────────────────────────────────
    LInt,    // Signed   64-bit
    LWord,   // Unsigned 64-bit
    Double,  // IEEE 754 Double precision 64-bit

    // ── Text ───────────────────────────────────────────────────
    String   // Chuỗi ASCII
}

/// <summary>
/// Thứ tự byte trong thanh ghi Modbus/PLC (áp dụng cho kiểu ≥ 16-bit)
/// </summary>
public enum PlcByteOrder
{
    /// <summary>High Byte First / High Word First — Big Endian (chuẩn mạng)</summary>
    HBHW,
    /// <summary>High Byte First / Low Word First — Word-swapped Big Endian</summary>
    HBLW,
    /// <summary>Low Byte First / High Word First — Word-swapped Little Endian</summary>
    LBHW,
    /// <summary>Low Byte First / Low Word First — Little Endian</summary>
    LBLW
}

/// <summary>Kết quả chuyển đổi PLC</summary>
public class PlcConversionResult
{
    public bool IsSuccess { get; set; }
    public string ResultValue { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public List<PlcConversionStep> Steps { get; set; } = new();
    public PlcDataType FromType { get; set; }
    public PlcDataType ToType { get; set; }
    public string InputValue { get; set; } = string.Empty;

    // Thông tin bổ sung về byte layout
    public List<RegisterRow> Registers { get; set; } = new();
    public string RawHex { get; set; } = string.Empty;

    // Bool-specific: hiển thị 16-bit grid
    public string? BoolHighByte { get; set; }  // chuỗi 8 ký tự '0'/'1' cho bit 15-8
    public string? BoolLowByte  { get; set; }  // chuỗi 8 ký tự '0'/'1' cho bit  7-0
    public ushort  BoolWord     { get; set; }
    public bool    IsBoolResult => BoolHighByte != null;
}

/// <summary>Một bước diễn giải</summary>
public class PlcConversionStep
{
    public int StepNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Formula { get; set; } = string.Empty;
    public string StepResult { get; set; } = string.Empty;
}

/// <summary>Một thanh ghi Modbus (16-bit) trong bảng byte-order</summary>
public class RegisterRow
{
    public int RegisterIndex { get; set; }
    public string HighByte { get; set; } = string.Empty;  // hex 2 ký tự
    public string LowByte { get; set; } = string.Empty;   // hex 2 ký tự
    public string RegisterHex { get; set; } = string.Empty; // 4 ký tự
    public string RegisterDec { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
}

/// <summary>Metadata mô tả một kiểu dữ liệu PLC</summary>
public class PlcDataTypeInfo
{
    public PlcDataType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Example { get; set; } = string.Empty;
    public string ValidChars { get; set; } = string.Empty;
    public int BitWidth { get; set; }
    public bool IsFloat { get; set; }
    public bool IsSigned { get; set; }
    public bool NeedsByteOrder { get; set; }
    public string Category { get; set; } = string.Empty;
}

/// <summary>Thông tin Byte Order</summary>
public class ByteOrderInfo
{
    public PlcByteOrder Order { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
