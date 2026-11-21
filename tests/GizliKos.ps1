# Testleri kullanıcının ekranına hiçbir şey çıkarmadan koşar: ayrı (görünmeyen) bir Windows masaüstü açılır,
# Calistir.ps1 orada çalışır. Açılır kutular / pencereler o masaüstünde kalır. Çıktı -Kayit dosyasına yazılır.
# Örnek: powershell -NoProfile -ExecutionPolicy Bypass -File tests\GizliKos.ps1 -Filtre verification
param(
    [string]$Filtre = '',
    [switch]$Derle,
    [string]$Kayit = (Join-Path $PSScriptRoot 'cikti\gizli-kosu.txt')
)

Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;

public static class HiddenDesktop
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct STARTUPINFO
    {
        public int cb; public string lpReserved; public string lpDesktop; public string lpTitle;
        public int dwX; public int dwY; public int dwXSize; public int dwYSize; public int dwXCountChars; public int dwYCountChars;
        public int dwFillAttribute; public int dwFlags; public short wShowWindow; public short cbReserved2;
        public IntPtr lpReserved2; public IntPtr hStdInput; public IntPtr hStdOutput; public IntPtr hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PROCESS_INFORMATION { public IntPtr hProcess; public IntPtr hThread; public int dwProcessId; public int dwThreadId; }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateDesktop(string name, IntPtr device, IntPtr devmode, int flags, uint access, IntPtr attributes);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool CloseDesktop(IntPtr desktop);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CreateProcess(string app, string cmd, IntPtr pa, IntPtr ta, bool inherit, uint flags, IntPtr env, string dir, ref STARTUPINFO si, out PROCESS_INFORMATION pi);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint WaitForSingleObject(IntPtr handle, uint ms);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetExitCodeProcess(IntPtr process, out uint code);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr handle);

    public static int Run(string desktopName, string commandLine, string workDir)
    {
        IntPtr desk = CreateDesktop(desktopName, IntPtr.Zero, IntPtr.Zero, 0, 0x10000000, IntPtr.Zero);
        if (desk == IntPtr.Zero) { throw new System.ComponentModel.Win32Exception(); }
        try
        {
            var si = new STARTUPINFO();
            si.cb = Marshal.SizeOf(typeof(STARTUPINFO));
            si.lpDesktop = desktopName;
            PROCESS_INFORMATION pi;
            if (!CreateProcess(null, commandLine, IntPtr.Zero, IntPtr.Zero, false, 0x08000000, IntPtr.Zero, workDir, ref si, out pi))
            {
                throw new System.ComponentModel.Win32Exception();
            }

            WaitForSingleObject(pi.hProcess, 0xFFFFFFFF);
            uint code;
            GetExitCodeProcess(pi.hProcess, out code);
            CloseHandle(pi.hThread);
            CloseHandle(pi.hProcess);
            return (int)code;
        }
        finally
        {
            CloseDesktop(desk);
        }
    }
}
'@

$root = Split-Path $PSScriptRoot -Parent
$runner = Join-Path $PSScriptRoot 'Calistir.ps1'
$args2 = ''
if ($Filtre.Length -gt 0) { $args2 += ' -Filtre ' + $Filtre }
if ($Derle) { $args2 += ' -Derle' }
New-Item -ItemType Directory -Force (Split-Path $Kayit) | Out-Null
$cmd = 'powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "& ''' + $runner + '''' + $args2 + ' *> ''' + $Kayit + '''"'
$code = [HiddenDesktop]::Run('PusulaTest', $cmd, $root)
Get-Content $Kayit | Select-String -Pattern 'OK\s+\d+|^\s+HATA|Toplam'
exit $code
