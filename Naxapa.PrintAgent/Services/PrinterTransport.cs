using System.Net.Sockets;
using System.Runtime.InteropServices;
using Naxapa.PrintAgent.Models;
namespace Naxapa.PrintAgent.Services;
public static class PrinterTransport
{
    public static async Task SendAsync(PrinterDto printer,byte[] data,CancellationToken ct)
    {
        if(printer.ConnectionType.Equals("REDE",StringComparison.OrdinalIgnoreCase))
        {
            if(string.IsNullOrWhiteSpace(printer.IpAddress)||printer.Port is null)throw new InvalidOperationException("Impressora de rede sem IP/porta.");
            using var client=new TcpClient();await client.ConnectAsync(printer.IpAddress,printer.Port.Value,ct);await client.GetStream().WriteAsync(data,ct);await client.GetStream().FlushAsync(ct);return;
        }
        var windowsName=printer.WindowsName;
        if(string.IsNullOrWhiteSpace(windowsName))throw new InvalidOperationException("Impressora sem nome configurado no Windows.");
        await Task.Run(()=>RawPrinter.Send(windowsName,data),ct);
    }
    private static class RawPrinter
    {
        [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]private sealed class DOC_INFO_1{[MarshalAs(UnmanagedType.LPWStr)]public string pDocName="NAXAPA";[MarshalAs(UnmanagedType.LPWStr)]public string? pOutputFile;[MarshalAs(UnmanagedType.LPWStr)]public string pDataType="RAW";}
        [DllImport("winspool.drv",SetLastError=true,CharSet=CharSet.Unicode)]static extern bool OpenPrinter(string pPrinterName,out IntPtr phPrinter,IntPtr pDefault);
        [DllImport("winspool.drv",SetLastError=true)]static extern bool ClosePrinter(IntPtr hPrinter);
        [DllImport("winspool.drv",SetLastError=true,CharSet=CharSet.Unicode)]static extern int StartDocPrinter(IntPtr hPrinter,int level,[In]DOC_INFO_1 di);
        [DllImport("winspool.drv",SetLastError=true)]static extern bool EndDocPrinter(IntPtr hPrinter);
        [DllImport("winspool.drv",SetLastError=true)]static extern bool StartPagePrinter(IntPtr hPrinter);
        [DllImport("winspool.drv",SetLastError=true)]static extern bool EndPagePrinter(IntPtr hPrinter);
        [DllImport("winspool.drv",SetLastError=true)]static extern bool WritePrinter(IntPtr hPrinter,IntPtr pBytes,int dwCount,out int dwWritten);
        public static void Send(string printerName,byte[] bytes){if(!OpenPrinter(printerName,out var h,IntPtr.Zero))throw Win32("OpenPrinter");try{var di=new DOC_INFO_1();if(StartDocPrinter(h,1,di)==0)throw Win32("StartDocPrinter");try{if(!StartPagePrinter(h))throw Win32("StartPagePrinter");var ptr=Marshal.AllocCoTaskMem(bytes.Length);try{Marshal.Copy(bytes,0,ptr,bytes.Length);if(!WritePrinter(h,ptr,bytes.Length,out var written)||written!=bytes.Length)throw Win32("WritePrinter");}finally{Marshal.FreeCoTaskMem(ptr);}EndPagePrinter(h);}finally{EndDocPrinter(h);}}finally{ClosePrinter(h);}}
        private static Exception Win32(string op)=>new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(),op);
    }
}
