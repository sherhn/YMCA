using System;
using System.Diagnostics; // Вот оно!
using System.IO;
using System.Windows.Forms;

namespace YMCA.Resources
{
    internal class MediaTools
    {
        public void getFrames(string mediapath, string framespath)
        {
            string ffmpegArgs = $"-i \"{mediapath}\" -vf fps=60 \"{Path.Combine(framespath, "frame_%04d.png")}\" -y";

            // Настройки процесса
            var processInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg.exe",
                Arguments = ffmpegArgs,
                CreateNoWindow = true,
                UseShellExecute = false
            };

            using (var process = Process.Start(processInfo))
            {
                process?.WaitForExit();
            }
        }
    }
}