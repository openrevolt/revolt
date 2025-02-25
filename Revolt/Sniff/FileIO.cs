using SharpPcap;
using SharpPcap.LibPcap;

namespace Revolt.Sniff;

public sealed partial class Sniffer {
 
    public void WriteToFile(string path) {
        using CaptureFileWriterDevice writer = new CaptureFileWriterDevice(path);
        writer.Open();
        writer.Write(new byte[] { });
    }

    public void ReadFromFile(string path) {
        using CaptureFileReaderDevice device = new CaptureFileReaderDevice(path);

        device.OnPacketArrival += Device_OnPacketArrival;
        device.Open();
        device.Capture();
    }

}