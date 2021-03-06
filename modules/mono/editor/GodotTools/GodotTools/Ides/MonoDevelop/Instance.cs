using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using GodotTools.Internals;
using GodotTools.Utils;

namespace GodotTools.Ides.MonoDevelop
{
    public class Instance
    {
        private readonly string solutionFile;
        private readonly EditorId editorId;

        private Process process;

        public bool IsRunning => process != null && !process.HasExited;

        public void Execute()
        {
            bool newWindow = process == null || process.HasExited;

            var args = new List<string>();

            string command;

            if (OS.IsOSX)
            {
                string bundleId = BundleIds[editorId];

                if (Internal.IsOsxAppBundleInstalled(bundleId))
                {
                    command = "open";

                    args.Add("-b");
                    args.Add(bundleId);

                    // The 'open' process must wait until the application finishes
                    if (newWindow)
                        args.Add("--wait-apps");

                    args.Add("--args");
                }
                else
                {
                    command = OS.PathWhich(ExecutableNames[editorId]);
                }
            }
            else
            {
                command = OS.PathWhich(ExecutableNames[editorId]);
            }

            args.Add("--ipc-tcp");

            if (newWindow)
                args.Add("\"" + Path.GetFullPath(solutionFile) + "\"");

            if (command == null)
                throw new FileNotFoundException();

            if (newWindow)
            {
                process = Process.Start(new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = string.Join(" ", args),
                    UseShellExecute = true
                });
            }
            else
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = string.Join(" ", args),
                    UseShellExecute = true
                })?.Dispose();
            }
        }

        public Instance(string solutionFile, EditorId editorId)
        {
            if (editorId == EditorId.VisualStudioForMac && !OS.IsOSX)
                throw new InvalidOperationException($"{nameof(EditorId.VisualStudioForMac)} not supported on this platform");

            this.solutionFile = solutionFile;
            this.editorId = editorId;
        }

        private static readonly IReadOnlyDictionary<EditorId, string> ExecutableNames;
        private static readonly IReadOnlyDictionary<EditorId, string> BundleIds;

        static Instance()
        {
            if (OS.IsOSX)
            {
                ExecutableNames = new Dictionary<EditorId, string>
                {
                    // Rely on PATH
                    {EditorId.MonoDevelop, "monodevelop"},
                    {EditorId.VisualStudioForMac, "VisualStudio"}
                };
                BundleIds = new Dictionary<EditorId, string>
                {
                    // TODO EditorId.MonoDevelop
                    {EditorId.VisualStudioForMac, "com.microsoft.visual-studio"}
                };
            }
            else if (OS.IsWindows)
            {
                ExecutableNames = new Dictionary<EditorId, string>
                {
                    // XamarinStudio is no longer a thing, and the latest version is quite old
                    // MonoDevelop is available from source only on Windows. The recommendation
                    // is to use Visual Studio instead. Since there are no official builds, we
                    // will rely on custom MonoDevelop builds being added to PATH.
                    {EditorId.MonoDevelop, "MonoDevelop.exe"}
                };
            }
            else if (OS.IsUnixLike())
            {
                ExecutableNames = new Dictionary<EditorId, string>
                {
                    // Rely on PATH
                    {EditorId.MonoDevelop, "monodevelop"}
                };
            }
        }
    }
}
