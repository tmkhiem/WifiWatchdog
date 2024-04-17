using ManagedNativeWifi;
using System.Net;
using System.Net.NetworkInformation;

namespace WlanWatchdog;

internal class Program
{
    static HttpClient httpClient = new HttpClient();

    static async Task Main()
    {
        int failedAttempts = 0;

        while (true)
        {
            bool connected = await Connected();
            if (!connected)
            {
                failedAttempts++;
                Console.WriteLine($"Connect failed ({failedAttempts}).");
            }
            else
            {
                failedAttempts = 0; // Reset counter if ping is successful
                Console.WriteLine("Connect successful.");
            }

            if (failedAttempts >= 10)
            {
                Console.WriteLine("10 successive connect failures. Reconnecting to WiFi...");

                // Disconnect from WiFi
                await DisconnectAsync();

                // Wait for 5 seconds before reconnecting
                do
                {
                    await Task.Delay(10000);
                    await ConnectAsync();
                }
                while (!await Connected());



                failedAttempts = 0; // Reset counter after reconnection
            }

            Thread.Sleep(10000); // Wait for 10 second before next ping
        }
    }

    private static async Task DisconnectAsync()
    {

        var networkInterface = NativeWifi.EnumerateInterfaces()
            .FirstOrDefault(i => i.State == InterfaceState.Connected);

        if (networkInterface is null)
        {
            Console.WriteLine("Disconnect: no connected network interface found.");
            return;
        }

        await NativeWifi.DisconnectNetworkAsync(
                interfaceId: networkInterface.Id,
                timeout: TimeSpan.FromSeconds(10));
    }

    private static async Task ConnectAsync()
    {
        Console.WriteLine("Trying to reconnect ...");

        var availableNetwork = NativeWifi.EnumerateAvailableNetworks()
            .Where(x => !string.IsNullOrWhiteSpace(x.ProfileName))
            .OrderByDescending(x => x.SignalQuality)
            .FirstOrDefault();

        if (availableNetwork is null)
            return;

        var result = await NativeWifi.ConnectNetworkAsync(
            interfaceId: availableNetwork.Interface.Id,
            profileName: availableNetwork.ProfileName,
            bssType: availableNetwork.BssType,
            timeout: TimeSpan.FromSeconds(10));

    }

    private static async Task<bool> Connected(int timeout=5000)
    {   
        using var cts = new CancellationTokenSource(timeout);
        try
        {
            var s = await httpClient.GetStringAsync(new Uri(File.ReadAllText("uri.txt")), cts.Token);
            return true;
        }
        catch (Exception e)
        {

            if (e.Message == "Response status code does not indicate success: 401 (Unauthorized).")
                return true;

            Console.WriteLine(e.Message);
            return false;
        }
    }



}
