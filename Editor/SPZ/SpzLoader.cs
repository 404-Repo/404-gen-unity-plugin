using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace GaussianSplatting.Editor
{
    public class SpzLoader
    {
        private static SpzLoader _instance;
        private const string WindowsLib = "spz_shared.dll";
        private const string MacLib = "libspz_shared.dylib";
        private const string LinuxLib = "libspz_shared.so";

        private SpzLoader() { }

        public static SpzLoader Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new SpzLoader();
                return _instance;
            }
        }

#if UNITY_STANDALONE_WIN
    private const string LIB_NAME = WindowsLib;
#elif UNITY_STANDALONE_OSX
    private const string LIB_NAME = MacLib;
#else
        private const string LIB_NAME = LinuxLib;
#endif

        [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern int decompress_spz(
            byte[] input,
            int inputSize,
            int includeNormals,
            out IntPtr outputPtr,
            out int outputSize
        );

        [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr get_error_string_spz(int code);

        [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void free_buffer_spz(IntPtr buffer);

        public byte[] Decompress(byte[] input, bool includeNormals = false)
        {
            IntPtr outputPtr = IntPtr.Zero;
            int outputSize = 0;

            int result = decompress_spz(input, input.Length, includeNormals ? 1 : 0, out outputPtr, out outputSize);
            if (result != 0)
            {
                string error = Marshal.PtrToStringAnsi(get_error_string_spz(result)) ?? $"Unknown error {result}";
                if (outputPtr != IntPtr.Zero)
                    free_buffer_spz(outputPtr);
                throw new Exception($"decompress_spz failed ({result}): {error}");
            }

            byte[] output = new byte[outputSize];
            Marshal.Copy(outputPtr, output, 0, outputSize);
            free_buffer_spz(outputPtr);

            return output;
        }
    }
}