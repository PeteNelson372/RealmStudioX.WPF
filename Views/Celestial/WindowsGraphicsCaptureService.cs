using System.Diagnostics;
using System.Runtime.InteropServices;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Windows.Foundation;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using WinRT;
using static Vortice.Direct3D11.D3D11;

namespace RealmStudioX.WPF.Views.Celestial
{
    public static class WindowsGraphicsCaptureService
    {
        private static readonly Guid GraphicsCaptureItemIid =
            new("79C3F95B-31F7-4EC2-A464-632EF5D30760");

        private const uint GA_ROOT = 2;

        [ComImport]
        [Guid("3628E81B-3CAC-4C60-B7F4-23CE0E0C3356")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IGraphicsCaptureItemInterop
        {
            void CreateForWindow(
                IntPtr window,
                ref Guid iid,
                out IntPtr result);

            void CreateForMonitor(
                IntPtr monitor,
                ref Guid iid,
                out IntPtr result);
        }

        [DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetAncestor(
            IntPtr hWnd,
            uint gaFlags);

        [DllImport("d3d11.dll", ExactSpelling = true)]
        private static extern int CreateDirect3D11DeviceFromDXGIDevice(
            IntPtr dxgiDevice,
            out IntPtr graphicsDevice);

        public static bool IsSupported()
        {
            return GraphicsCaptureSession.IsSupported();
        }

        public static GraphicsCaptureItem CreateItemForWindow(
            IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero)
                throw new ArgumentException(
                    "The window handle is zero.", nameof(hwnd));

            bool isWindow = IsWindow(hwnd);
            bool isVisible = IsWindowVisible(hwnd);
            bool isMinimized = IsIconic(hwnd);
            bool isRootWindow = GetAncestor(hwnd, GA_ROOT) == hwnd;

            Debug.WriteLine("Windows Graphics Capture diagnostics:");
            Debug.WriteLine($"  HWND: 0x{hwnd.ToInt64():X}");
            Debug.WriteLine($"  IsWindow: {isWindow}");
            Debug.WriteLine($"  IsWindowVisible: {isVisible}");
            Debug.WriteLine($"  IsIconic: {isMinimized}");
            Debug.WriteLine($"  IsRootWindow: {isRootWindow}");

            if (!isWindow || !isRootWindow)
                throw new InvalidOperationException(
                    "The supplied HWND is not a valid root window.");

            if (!isVisible || isMinimized)
                throw new InvalidOperationException(
                    "The window must be visible and not minimized.");

            IGraphicsCaptureItemInterop interop =
                GraphicsCaptureItem.As<IGraphicsCaptureItemInterop>();

            Guid iid = GraphicsCaptureItemIid;
            IntPtr itemPointer = IntPtr.Zero;

            try
            {
                interop.CreateForWindow(
                    hwnd, ref iid, out itemPointer);

                if (itemPointer == IntPtr.Zero)
                    throw new InvalidOperationException(
                        "Windows Graphics Capture returned a null item.");

                return MarshalInterface<GraphicsCaptureItem>
                    .FromAbi(itemPointer);
            }
            finally
            {
                if (itemPointer != IntPtr.Zero)
                    Marshal.Release(itemPointer);
            }
        }

        public static IDirect3DDevice CreateDirect3DDevice()
        {
            using ID3D11Device d3dDevice =
                D3D11CreateDevice(
                    DriverType.Hardware,
                    DeviceCreationFlags.BgraSupport);

            using IDXGIDevice dxgiDevice =
                d3dDevice.QueryInterface<IDXGIDevice>();

            IntPtr graphicsDevicePointer = IntPtr.Zero;

            try
            {
                int hr = CreateDirect3D11DeviceFromDXGIDevice(
                    dxgiDevice.NativePointer,
                    out graphicsDevicePointer);

                Marshal.ThrowExceptionForHR(hr);

                if (graphicsDevicePointer == IntPtr.Zero)
                    throw new InvalidOperationException(
                        "Failed to create the WinRT Direct3D device.");

                return MarshalInterface<IDirect3DDevice>
                    .FromAbi(graphicsDevicePointer);
            }
            finally
            {
                if (graphicsDevicePointer != IntPtr.Zero)
                    Marshal.Release(graphicsDevicePointer);
            }
        }


        public static async Task<byte[]> CaptureWindowPngAsync(
            IntPtr hwnd,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            if (!GraphicsCaptureSession.IsSupported())
            {
                throw new NotSupportedException(
                    "Windows Graphics Capture is not supported on this system.");
            }

            var item = CreateItemForWindow(hwnd);
            var device = CreateDirect3DDevice();

            Windows.Graphics.SizeInt32 size = item.Size;

            if (size.Width <= 0 || size.Height <= 0)
            {
                throw new InvalidOperationException(
                    "The capture item has an invalid size.");
            }

            using var framePool =
                Direct3D11CaptureFramePool.CreateFreeThreaded(
                    device,
                    DirectXPixelFormat.B8G8R8A8UIntNormalized,
                    2,
                    size);

            using var session = framePool.CreateCaptureSession(item);

            var completion = new TaskCompletionSource<byte[]>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            TypedEventHandler<Direct3D11CaptureFramePool, object> handler =
                async (sender, args) =>
                {
                    try
                    {
                        using Direct3D11CaptureFrame? frame =
                            sender.TryGetNextFrame();

                        if (frame is null)
                            return;

                        using SoftwareBitmap bitmap =
                            await SoftwareBitmap.CreateCopyFromSurfaceAsync(
                                frame.Surface);

                        byte[] png = await EncodePngAsync(bitmap);

                        completion.TrySetResult(png);
                    }
                    catch (Exception ex)
                    {
                        completion.TrySetException(ex);
                    }
                };

            framePool.FrameArrived += handler;

            try
            {
                session.StartCapture();

                return await completion.Task.WaitAsync(
                    timeout ?? TimeSpan.FromSeconds(15),
                    cancellationToken);
            }
            finally
            {
                framePool.FrameArrived -= handler;
            }
        }

        private static async Task<byte[]> EncodePngAsync(
            SoftwareBitmap bitmap)
        {
            using var stream = new InMemoryRandomAccessStream();

            BitmapEncoder encoder = await BitmapEncoder.CreateAsync(
                BitmapEncoder.PngEncoderId,
                stream);

            encoder.SetSoftwareBitmap(bitmap);

            await encoder.FlushAsync();

            stream.Seek(0);

            using var reader = new DataReader(stream.GetInputStreamAt(0));

            uint byteCount = checked((uint)stream.Size);

            await reader.LoadAsync(byteCount);

            byte[] result = new byte[byteCount];

            reader.ReadBytes(result);

            return result;
        }

    }
}
