using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stemmer.Cvb;
using Stemmer.Cvb.Driver;
using Stemmer.Cvb.Utilities;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Stemmer.Cvb.Async;
using static Stemmer.Cvb.Image;

namespace Isolated
{
    public sealed class CameraConnection : IDisposable
    {
        private Device _camera;
        private readonly CancellationToken _cancellationToken;
        private CameraConnection(Device camera, CancellationToken cancellationToken)
        {
            _camera = camera;
            _cancellationToken = cancellationToken;
            _camera.ConnectionStateChanged += camera_ConnectionStateChanged;
        }
        public bool IsAlive()
        {
            return _camera.ConnectionState == ConnectionState.Connected;
        }
        public static CameraConnection EstablishAsync(CancellationToken cancellationToken, bool alwaysOnGrab = false)
        {
            string driverString = Environment.ExpandEnvironmentVariables("%CVB%") + @"\Drivers\GenICam.vin";
            Device camera = DeviceFactory.OpenPort(driverString, 0);
            if (alwaysOnGrab)
            {
                camera.Stream.Start();
            }
            return new CameraConnection(camera, cancellationToken);
        }

        private void camera_ConnectionStateChanged(object sender, ConnectionStateChangeEventArgs e)
        {
            Console.WriteLine($"_camera_ConnectionStateChanged : {_camera.ConnectionState}");
            if (_camera.ConnectionState == ConnectionState.Disconnected)
            {
                Dispose();
            }
        }

        public async void Dispose()
        {
            try
            {
                while (true)
                {
                    var result = _camera.Stream.TryAbort();
                    if (result)
                    {
                        _camera.Stream.Dispose();
                        _camera.Dispose();
                        break;
                    }
                }
            }
            catch (Exception)
            {

                Console.WriteLine("Exception in Dispose");
            }

        }
        public void Grab(int imgNum, bool alwaysOnGrab = false)
        {
            string directoryPath = $"D:\\Grabber projects\\Images\\{DateTime.Now.Ticks}";
            Directory.CreateDirectory(directoryPath);
            if (!alwaysOnGrab)
            {
                _camera.Stream.Start();
            }
            for (int i = 0; i < imgNum; i++)
            {
                try
                {
                    using (var image = _camera.Stream.Wait(out var status))
                    {

                        if (status == WaitStatus.Ok)
                        {
                            string imageName = $"{i}.png";

                            var path = Path.Combine(directoryPath, imageName);
                            image.Save(path);
                        }
                        else if (status == WaitStatus.Abort)
                        {
                            Console.WriteLine($"Acquisition error because grab was aborted");
                            return;
                        }
                        else if (status == WaitStatus.Timeout)
                        {
                            Console.WriteLine($"Acquisition timeout");
                        }
                    }
                }
                catch (Exception)
                {
                    string driverString = Environment.ExpandEnvironmentVariables("%CVB%") + @"\Drivers\GenICam.vin";
                    try
                    {
                        _camera = DeviceFactory.OpenPort(driverString, 0);
                    }
                    catch (Exception ex)
                    {

                        Console.WriteLine(ex.Message);
                    }
                }
            }
            if (!alwaysOnGrab)
            {
                _camera.Stream.Abort();
            }
        }
    }
}
