namespace mirabox;

/// <summary>
/// Interfejs dla różnych implementacji komunikacji z Mirabox
/// </summary>
public interface IMiraboxReader
{
    bool WriteData(byte[] data, bool removeReportId);
}
