using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace CloudDiscoLauncher
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string bridgeDir = Path.Combine(baseDir, "TikTokBridge");
            string nodeModulesDir = Path.Combine(bridgeDir, "node_modules");
            string unityExe = Path.Combine(baseDir, "TikTokLiveGameUnity.exe");

            // Check if node is installed
            if (!IsNodeInstalled())
            {
                MessageBox.Show(
                    "Không tìm thấy Node.js trên máy tính!\n\nVui lòng tải và cài đặt Node.js (phiên bản 20 trở lên) từ https://nodejs.org/ rồi thử lại.",
                    "Cloud Disco - Thiếu Node.js",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }

            Process nodeProcess = null;
            try
            {
                // 1. Check & install npm packages if node_modules missing
                if (!Directory.Exists(nodeModulesDir))
                {
                    ProcessStartInfo npmInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = "/c npm install",
                        WorkingDirectory = bridgeDir,
                        UseShellExecute = false,
                        CreateNoWindow = false
                    };
                    Process npmProc = Process.Start(npmInfo);
                    if (npmProc != null)
                    {
                        npmProc.WaitForExit();
                    }
                }

                // 2. Start Node server in background
                ProcessStartInfo nodeInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c node server.js",
                    WorkingDirectory = bridgeDir,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                nodeProcess = Process.Start(nodeInfo);

                // Wait 1.5s for server to start
                Thread.Sleep(1500);

                // 3. Open Control Web Page in Browser
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "http://127.0.0.1:3000/control.html",
                        UseShellExecute = true
                    });
                }
                catch { }

                // 4. Start Unity Game
                if (File.Exists(unityExe))
                {
                    ProcessStartInfo unityInfo = new ProcessStartInfo
                    {
                        FileName = unityExe,
                        WorkingDirectory = baseDir,
                        UseShellExecute = true
                    };
                    Process unityProc = Process.Start(unityInfo);
                    if (unityProc != null)
                    {
                        unityProc.WaitForExit();
                    }
                }
                else
                {
                    MessageBox.Show(
                        "Không tìm thấy file 'TikTokLiveGameUnity.exe' trong thư mục game!",
                        "Cloud Disco - Lỗi",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Lỗi khi khởi chạy Cloud Disco:\n\n" + ex.Message,
                    "Cloud Disco - Lỗi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
            finally
            {
                // Kill Node process tree on exit
                if (nodeProcess != null && !nodeProcess.HasExited)
                {
                    KillProcessTree(nodeProcess.Id);
                }
            }
        }

        static bool IsNodeInstalled()
        {
            try
            {
                ProcessStartInfo checkNode = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c node -v",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using (Process p = Process.Start(checkNode))
                {
                    p.WaitForExit();
                    return p.ExitCode == 0;
                }
            }
            catch
            {
                return false;
            }
        }

        static void KillProcessTree(int pid)
        {
            try
            {
                ProcessStartInfo killInfo = new ProcessStartInfo
                {
                    FileName = "taskkill",
                    Arguments = string.Format("/F /T /PID {0}", pid),
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using (Process killProc = Process.Start(killInfo))
                {
                    killProc.WaitForExit();
                }
            }
            catch { }
        }
    }
}
