using System.Runtime.InteropServices;

namespace Revolt.Sniff;
public static class PCap {

    [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Auto)]
    static extern IntPtr LoadLibrary(string lpLibFileName);

    public static void Init() {
#if !DEBUG
        if (!OperatingSystem.IsWindows()) return;
        string basePath = Directory.GetCurrentDirectory();
        string wpcapPath = ExtractResource(basePath, "wpcap.dll");
        string packetPath = ExtractResource(basePath, "Packet.dll");
        LoadLibrary(wpcapPath);
        LoadLibrary(packetPath);
#endif
    }

    static string ExtractResource(string outputPath, string resourceName) {
        string fullPath = Path.Combine(outputPath, resourceName);
        if (!File.Exists(fullPath)) {
            byte[] resourceBytes = (byte[])Properties.Resources.ResourceManager.GetObject(Path.GetFileNameWithoutExtension(resourceName));
            File.WriteAllBytes(fullPath, resourceBytes);
        }
        return fullPath;
    }
}
