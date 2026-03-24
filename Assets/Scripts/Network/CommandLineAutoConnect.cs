using System;
using UnityEngine;

namespace AsylumHorror.Network
{
    public class CommandLineAutoConnect : MonoBehaviour
    {
        private bool launched;

        private void Start()
        {
            if (launched)
            {
                return;
            }

            launched = true;
            TryAutoConnectFromArguments();
        }

        private void TryAutoConnectFromArguments()
        {
            HorrorNetworkManager manager = HorrorNetworkManager.Instance;
            if (manager == null)
            {
                return;
            }

            string[] args = Environment.GetCommandLineArgs();
            bool host = false;
            string joinTarget = null;
            ushort? explicitPort = null;

            for (int index = 0; index < args.Length; index++)
            {
                string arg = args[index];
                if (string.IsNullOrWhiteSpace(arg))
                {
                    continue;
                }

                if (string.Equals(arg, "--host", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(arg, "-host", StringComparison.OrdinalIgnoreCase))
                {
                    host = true;
                    continue;
                }

                if (arg.StartsWith("--join=", StringComparison.OrdinalIgnoreCase))
                {
                    joinTarget = arg.Substring("--join=".Length);
                    continue;
                }

                if (string.Equals(arg, "--join", StringComparison.OrdinalIgnoreCase) && index + 1 < args.Length)
                {
                    joinTarget = args[index + 1];
                    index++;
                    continue;
                }

                if (arg.StartsWith("--port=", StringComparison.OrdinalIgnoreCase))
                {
                    if (ushort.TryParse(arg.Substring("--port=".Length), out ushort parsedPort))
                    {
                        explicitPort = parsedPort;
                    }
                    continue;
                }

                if (string.Equals(arg, "--port", StringComparison.OrdinalIgnoreCase) && index + 1 < args.Length)
                {
                    if (ushort.TryParse(args[index + 1], out ushort parsedPort))
                    {
                        explicitPort = parsedPort;
                    }
                    index++;
                }
            }

            if (host)
            {
                manager.StartHostFromMenu(explicitPort ?? manager.DefaultPort);
                return;
            }

            if (!string.IsNullOrWhiteSpace(joinTarget))
            {
                if (explicitPort.HasValue)
                {
                    manager.StartClientFromMenu(joinTarget, explicitPort.Value);
                }
                else
                {
                    manager.StartClientFromMenu(joinTarget);
                }
            }
        }
    }
}
