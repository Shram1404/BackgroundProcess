using Microsoft.Win32.SafeHandles;
using System.IO.Pipes;
using System.Runtime.InteropServices;

namespace MyBackgroundService
{
    public class NativePipeServer // GPT Code
    {
        // Оголошення P/Invoke для CreateNamedPipe
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern SafePipeHandle CreateNamedPipe(
            string lpName,
            uint dwOpenMode,
            uint dwPipeMode,
            uint nMaxInstances,
            uint nOutBufferSize,
            uint nInBufferSize,
            uint nDefaultTimeOut,
            ref SECURITY_ATTRIBUTES lpSecurityAttributes);

        // Функція для конвертації SDDL у SECURITY_DESCRIPTOR
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool ConvertStringSecurityDescriptorToSecurityDescriptor(
            string StringSecurityDescriptor,
            uint StringSDRevision,
            out IntPtr SecurityDescriptor,
            IntPtr SecurityDescriptorSize);

        // Для звільнення пам'яті, виділеної ConvertStringSecurityDescriptorToSecurityDescriptor
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LocalFree(IntPtr hMem);

        // Структура SECURITY_ATTRIBUTES
        [StructLayout(LayoutKind.Sequential)]
        private struct SECURITY_ATTRIBUTES
        {
            public int nLength;
            public IntPtr lpSecurityDescriptor;
            public bool bInheritHandle;
        }

        // Константи для CreateNamedPipe
        private const uint PIPE_ACCESS_DUPLEX = 0x00000003;
        private const uint FILE_FLAG_OVERLAPPED = 0x40000000;
        private const uint PIPE_TYPE_MESSAGE = 0x00000004;
        private const uint PIPE_READMODE_MESSAGE = 0x00000002;
        private const uint PIPE_WAIT = 0x00000000;

        /// <summary>
        /// Створює NamedPipeServerStream із заданим ім'ям каналу та налаштованим доступом для всіх.
        /// </summary>
        /// <param name="pipeName">Ім'я каналу (без префікса "\\.\pipe\")</param>
        /// <returns>NamedPipeServerStream з потрібними параметрами безпеки</returns>
        public static NamedPipeServerStream CreatePipeServer(string pipeName)
        {
            // Використовуємо SDDL для дозволу повного доступу (Generic All) для всіх користувачів
            string sddl = "D:(A;;FA;;;WD)";

            // Конвертуємо SDDL у SECURITY_DESCRIPTOR
            if (!ConvertStringSecurityDescriptorToSecurityDescriptor(sddl, 1, out IntPtr pSecurityDescriptor, IntPtr.Zero))
            {
                int error = Marshal.GetLastWin32Error();
                throw new System.ComponentModel.Win32Exception(error, "ConvertStringSecurityDescriptorToSecurityDescriptor не вдалося");
            }

            // Заповнюємо SECURITY_ATTRIBUTES
            var sa = new SECURITY_ATTRIBUTES
            {
                nLength = Marshal.SizeOf(typeof(SECURITY_ATTRIBUTES)),
                lpSecurityDescriptor = pSecurityDescriptor,
                bInheritHandle = false
            };

            // Формуємо повне ім'я каналу
            string fullPipeName = @"\\.\pipe\" + pipeName;

            // Створюємо іменований канал з асинхронним режимом роботи
            SafePipeHandle pipeHandle = CreateNamedPipe(
                fullPipeName,
                PIPE_ACCESS_DUPLEX | FILE_FLAG_OVERLAPPED,
                PIPE_TYPE_MESSAGE | PIPE_READMODE_MESSAGE | PIPE_WAIT,
                1,        // максимальна кількість інстанцій
                4096,     // розмір вихідного буфера (байт)
                4096,     // розмір вхідного буфера (байт)
                0,        // таймаут (мс)
                ref sa);

            // Звільняємо пам'ять, зайняту конвертованим SECURITY_DESCRIPTOR
            LocalFree(pSecurityDescriptor);

            if (pipeHandle.IsInvalid)
            {
                int error = Marshal.GetLastWin32Error();
                throw new System.ComponentModel.Win32Exception(error, "CreateNamedPipe не вдалося");
            }

            // Обгортаємо SafePipeHandle в NamedPipeServerStream.
            // Використовуємо конструктор: NamedPipeServerStream(PipeDirection, bool isAsync, bool isConnected, SafePipeHandle)
            return new NamedPipeServerStream(PipeDirection.InOut, true, false, pipeHandle);
        }
    }
}
