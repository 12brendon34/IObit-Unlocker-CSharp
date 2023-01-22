using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace IObit_Unlocker_CSharp
{
    public static class IObitUnlocker
    {
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern SafeFileHandle CreateFile(string lpFileName,
            uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes,
            uint dwCreationDisposition, uint dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        [DllImport("kernel32")]
        public static extern IntPtr VirtualAlloc(
            IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int MessageBoxW(
            int hWnd, string text, string caption, uint type);

        [DllImport("advapi32.dll", EntryPoint = "OpenSCManagerW",
            ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr OpenSCManager(
            string machineName, string databaseName, uint dwAccess);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr CreateService(IntPtr hSCManager,
            string lpServiceName, string lpDisplayName, uint dwDesiredAccess,
            uint dwServiceType, uint dwStartType, uint dwErrorControl,
            string lpBinaryPathName, [Optional] string lpLoadOrderGroup,
            [Optional] string lpdwTagId, [Optional] string lpDependencies,
            [Optional] string lpServiceStartName, [Optional] string lpPassword);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseServiceHandle(IntPtr hSCObject);

        [DllImport("advapi32.dll", EntryPoint = "OpenServiceA", SetLastError = true,
            CharSet = CharSet.Ansi)]
        private static extern IntPtr OpenService(
            IntPtr hSCManager, string lpServiceName, uint dwDesiredAccess);

        [DllImport("advapi32.dll", EntryPoint = "ChangeServiceConfig")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ChangeServiceConfig(IntPtr hService,
            uint dwServiceType, uint dwStartType, uint dwErrorControl,
            string lpBinaryPathName, string lpLoadOrderGroup, string lpdwTagId,
            string lpDependencies, string lpServiceStartName, string lpPassword,
            string lpDisplayName);

        [DllImport("advapi32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool StartService(
            IntPtr hService, IntPtr dwNumServiceArgs, string[] lpServiceArgVectors);

        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true,
            CharSet = CharSet.Auto)]
        private static extern bool DeviceIoControl(SafeFileHandle hDevice,
            uint dwIoControlCode, IntPtr lpInBuffer, uint nInBufferSize,
            IntPtr lpOutBuffer, uint nOutBufferSize, out uint lpBytesReturned,
            IntPtr lpOverlapped);

        public const uint STANDARD_RIGHTS_REQUIRED = 0x000F0000;
        public const uint SC_MANAGER_CONNECT = 0x0001;
        public const uint SC_MANAGER_CREATE_SERVICE = 0x0002;
        public const uint SC_MANAGER_ENUMERATE_SERVICE = 0x0004;
        public const uint SC_MANAGER_LOCK = 0x0008;
        public const uint SC_MANAGER_QUERY_LOCK_STATUS = 0x0010;
        public const uint SC_MANAGER_MODIFY_BOOT_CONFIG = 0x0020;

        public const uint SC_MANAGER_ALL_ACCESS = STANDARD_RIGHTS_REQUIRED
            | SC_MANAGER_CONNECT | SC_MANAGER_CREATE_SERVICE
            | SC_MANAGER_ENUMERATE_SERVICE | SC_MANAGER_LOCK
            | SC_MANAGER_QUERY_LOCK_STATUS | SC_MANAGER_MODIFY_BOOT_CONFIG;

        public const uint SC_STATUS_PROCESS_INFO = 0;

        public const uint SERVICE_KERNEL_DRIVER = 0x00000001;
        public const uint SERVICE_DEMAND_START = 0x00000003;
        public const uint SERVICE_DISABLED = 0x00000004;
        public const uint SERVICE_ERROR_NORMAL = 0x00000001;

        public const uint SERVICE_QUERY_CONFIG = 0x0001;
        public const uint SERVICE_CHANGE_CONFIG = 0x0002;
        public const uint SERVICE_QUERY_STATUS = 0x0004;
        public const uint SERVICE_ENUMERATE_DEPENDENTS = 0x0008;
        public const uint SERVICE_START = 0x0010;
        public const uint SERVICE_STOP = 0x0020;
        public const uint SERVICE_PAUSE_CONTINUE = 0x0040;
        public const uint SERVICE_INTERROGATE = 0x0080;
        public const uint SERVICE_USER_DEFINED_CONTROL = 0x0100;

        public const uint SERVICE_NO_CHANGE = 0xffffffff;

        public const uint SERVICE_CONTROL_STOP = 0x00000001;

        public const uint ERROR_SERVICE_EXISTS = 1073;
        public const uint ERROR_SERVICE_ALREADY_RUNNING = 1056;


        public const uint SERVICE_ALL_ACCESS = STANDARD_RIGHTS_REQUIRED
            | SERVICE_QUERY_CONFIG | SERVICE_CHANGE_CONFIG | SERVICE_QUERY_STATUS
            | SERVICE_ENUMERATE_DEPENDENTS | SERVICE_START | SERVICE_STOP
            | SERVICE_PAUSE_CONTINUE | SERVICE_INTERROGATE
            | SERVICE_USER_DEFINED_CONTROL;

        public const uint DELETE = 0x00010000;

        public const uint GENERIC_READ = 0x80000000;
        public const uint GENERIC_WRITE = 0x40000000;
        public const uint OPEN_EXISTING = 3;
        private static readonly string SVCNAME = "IObitUnlocker";

        public static SafeFileHandle hDriver;
        public static IntPtr input_buffer;
        public static IntPtr output_buffer;

        public const uint input_buffer_size = 0x1000;
        public const uint output_buffer_size = 0x1000;

        public const uint dwIoctl_action = 0x222124;
        public static uint dwBytesOut = 0;

        public const uint MEM_COMMIT = 0x00001000;
        public const uint MEM_RESERVE = 0x00002000;
        public const uint PAGE_EXECUTE_READWRITE = 0x40;

        public const uint MB_OK = 0x00000000;
        // no clue why im inporting messagebox from user32.dll instead
        // of using some c# one
        private static string IntPtrToAddress(IntPtr P)
        {
            return string.Format("0x{0:X}", (int)P);
        }

        public static void Delete(string path)
        {
            if (hDriver == null)
            {
                CreateDriverInstance();
            }

            if (!hDriver.IsInvalid)
            {
                //write path to mem
                Marshal.Copy(path.ToCharArray(), 0, input_buffer, path.Length);

                //write bytes to mem (tells what operation it should do) (Delete)
                IntPtr target_addr = input_buffer + 0x420;
                byte[] DelOpBytes = new byte[] { 0x1, 0x0, 0x0, 0x0, 0x3 };
                Marshal.Copy(DelOpBytes, 0, target_addr, DelOpBytes.Length);

                DeviceIoControl(hDriver,
                    dwIoctl_action,
                    input_buffer,
                    input_buffer_size,
                    output_buffer,
                    output_buffer_size,
                    out dwBytesOut,
                    IntPtr.Zero);
            }
            else
            {
                Console.WriteLine("[-] Failed To Open Driver - Quitting");
                MessageBoxW(0, "Failed To Open Driver\nQuitting", "Error", MB_OK);
                Environment.Exit(-1);
            }
        }

        public static void Move(string path, string pathTo) { 
        
            if (hDriver == null)
            {
                CreateDriverInstance();
            }

            if (!hDriver.IsInvalid)
            {
                //write first path to mem
                Marshal.Copy(path.ToCharArray(), 0, input_buffer, path.Length);

                IntPtr MoveToPathAddress = input_buffer + 0x210;
                //write seccond path to mem 
                Marshal.Copy(pathTo.ToCharArray(), 0, MoveToPathAddress, pathTo.Length);

                //write bytes to mem (tells what operation it should do) (Move) path -> pathTo
                IntPtr target_addr = input_buffer + 0x420;
                byte[] MovOpBytes = new byte[] { 0x3, 0x0, 0x0, 0x0, 0x3 };
                Marshal.Copy(MovOpBytes, 0, target_addr, MovOpBytes.Length);

                DeviceIoControl(hDriver,
                    dwIoctl_action,
                    input_buffer,
                    input_buffer_size,
                    output_buffer,
                    output_buffer_size,
                    out dwBytesOut,
                    IntPtr.Zero);
            }
            else
            {
                Console.WriteLine("[-] Failed To Open Driver - Quitting");
                MessageBoxW(0, "Failed To Open Driver\nQuitting", "Error", MB_OK);
                Environment.Exit(-1);
            }
        }

        public static void CreateDriverInstance()
        {
            hDriver = CreateFile("\\\\.\\IOBitUnlockerDevice",
                GENERIC_READ | GENERIC_WRITE,
                0,
                IntPtr.Zero,
                OPEN_EXISTING,
                0,
                IntPtr.Zero);

            if (!hDriver.IsInvalid)
            {
                Console.WriteLine("Opened Driver Handle");

                input_buffer = VirtualAlloc(IntPtr.Zero,
                    input_buffer_size,
                    MEM_RESERVE | MEM_COMMIT,
                    PAGE_EXECUTE_READWRITE);

                if (input_buffer == IntPtr.Zero)
                {
                    Console.WriteLine("Failed to allocate input buffer memory");
                    MessageBoxW(
                      0,
                      "Unable to allocate memory for input buffer\nQuitting",
                      "Error",
                      MB_OK);
                    Environment.Exit(-1);
                }

                Console.WriteLine("Allocated input buffer at: " + IntPtrToAddress(input_buffer));

                output_buffer = VirtualAlloc(IntPtr.Zero,
                    output_buffer_size,
                    MEM_RESERVE | MEM_COMMIT,
                    PAGE_EXECUTE_READWRITE);

                if (output_buffer == IntPtr.Zero)
                {
                    Console.WriteLine("Failed to allocate output buffer memory");
                    MessageBoxW(
                      0,
                      "Unable to allocate memory for Console buffer\nQuitting",
                      "Error",
                      MB_OK); 
                    Environment.Exit(-1);
                }

                Console.WriteLine("Allocated output buffer at: " + IntPtrToAddress(output_buffer));
            }
            else
            {
                Console.WriteLine("Failed To Open Driver - Quitting");
                Environment.Exit(-1);
            }
        }

        // https://learn.microsoft.com/en-us/windows/win32/services/service-configuration-program-tasks
        public static void SvcInstall(string DriverPath)
        {
            IntPtr schSCManager;
            IntPtr schService;

            if (!File.Exists(DriverPath))
            {
                Console.WriteLine("IObitUnlocker.sys does not exist.");
                Environment.Exit(-1);
            }

            // Requires Elevation or returns error 5
            schSCManager = OpenSCManager(null, // local computer
                                         null, // ServicesActive database
                                         SC_MANAGER_ALL_ACCESS); // full access rights

            if (schSCManager == IntPtr.Zero)
            {
                Console.WriteLine("OpenSCManager failed " + Marshal.GetLastWin32Error());
                return;
            }

            // Create the service
            schService = CreateService(schSCManager,          // SCM database
                SVCNAME,               // name of service
                SVCNAME,               // service name to display
                SERVICE_ALL_ACCESS,    // desired access
                SERVICE_KERNEL_DRIVER, // service type
                SERVICE_DEMAND_START,  // start type
                SERVICE_ERROR_NORMAL,  // error control type
                DriverPath,      // path to service's binary
                null,     // no load ordering group
                null,     // no tag identifier
                null,     // no dependencies
                null,     // LocalSystem account
                SVCNAME); // lpszServiceName?  // no password

            if (schService == IntPtr.Zero)
            {
                if (ERROR_SERVICE_EXISTS == Marshal.GetLastWin32Error())
                {
                    Console.WriteLine("Service Already Exists");
                    CloseServiceHandle(schSCManager);
                }
                else
                {
                    Console.WriteLine("CreateService failed " + Marshal.GetLastWin32Error());
                    CloseServiceHandle(schSCManager);
                }
                return;
            }
            else
            {
                Console.WriteLine("Service installed successfully. " + IntPtrToAddress(schService));
            }

            CloseServiceHandle(schService);
            CloseServiceHandle(schSCManager);
        }
        public static void EnableSvc()
        {
            IntPtr schSCManager;
            IntPtr schService;

            // Get a handle to the SCM database.

            // Requires Elevation or returns error 5
            schSCManager = OpenSCManager(null, // local computer
                                         null, // ServicesActive database
                                         SC_MANAGER_ALL_ACCESS); // full access rights

            if (schSCManager == IntPtr.Zero)
            {
                Console.WriteLine("OpenSCManager failed " + Marshal.GetLastWin32Error());
                return;
            }

            // Get a handle to the service.

            schService =
              OpenService(schSCManager,           // SCM database
                          SVCNAME,                // name of service
                          SERVICE_CHANGE_CONFIG); // need change config access

            if (schService == IntPtr.Zero)
            {
                Console.WriteLine("OpenService failed " + Marshal.GetLastWin32Error());
                CloseServiceHandle(schSCManager);
                return;
            }

            // Change the service start type.

            if (!ChangeServiceConfig(schService,           // handle of service
                SERVICE_NO_CHANGE,    // service type: no change
                SERVICE_DEMAND_START, // service start type
                SERVICE_NO_CHANGE,    // error control: no change
                null,                 // binary path: no change
                null,  // load order group: no change
                null,  // tag ID: no change
                null,  // dependencies: no change
                null,  // account name: no change
                null,  // password: no change
                null)) // display name: no change
            {
                Console.WriteLine("ChangeServiceConfig failed " +
                                  Marshal.GetLastWin32Error());
            }
            else
            {
                Console.WriteLine("Service Enabled Successfully.");
            }

            CloseServiceHandle(schService);
            CloseServiceHandle(schSCManager);
        }
        public static void DisableSvc()
        {
            IntPtr schSCManager;
            IntPtr schService;

            // Get a handle to the SCM database.

            // Requires Elevation or returns error 5
            schSCManager = OpenSCManager(null, // local computer
                                         null, // ServicesActive database
                                         SC_MANAGER_ALL_ACCESS); // full access rights

            if (schSCManager == IntPtr.Zero)
            {
                Console.WriteLine("OpenSCManager failed " + Marshal.GetLastWin32Error());
                return;
            }

            // Get a handle to the service.

            schService =
              OpenService(schSCManager,           // SCM database
                          SVCNAME,                // name of service
                          SERVICE_CHANGE_CONFIG); // need change config access

            if (schService == IntPtr.Zero)
            {
                Console.WriteLine("OpenService failed " + Marshal.GetLastWin32Error());
                CloseServiceHandle(schSCManager);
                return;
            }

            // Change the service start type.

            if (!ChangeServiceConfig(schService,        // handle of service
                SERVICE_NO_CHANGE, // service type: no change
                SERVICE_DISABLED,  // service start type
                SERVICE_NO_CHANGE, // error control: no change
                null,              // binary path: no change
                null,              // load order group: no change
                null,              // tag ID: no change
                null,              // dependencies: no change
                null,              // account name: no change
                null,              // password: no change
                null))             // display name: no change
            {
                Console.WriteLine("ChangeServiceConfig failed " +
                                  Marshal.GetLastWin32Error());
            }
            else
            {
                Console.WriteLine("Service Disabled Successfully.");
            }

            CloseServiceHandle(schService);
            CloseServiceHandle(schSCManager);
        }
        public static void StartSvc()
        {
            IntPtr schSCManager;
            IntPtr schService;

            // Get a handle to the SCM database.

            // Requires Elevation or returns error 5
            schSCManager = OpenSCManager(null, // local computer
                                         null, // ServicesActive database
                                         SC_MANAGER_ALL_ACCESS); // full access rights

            if (schSCManager == IntPtr.Zero)
            {
                Console.WriteLine("OpenSCManager failed " + Marshal.GetLastWin32Error());
                return;
            }

            // Get a handle to the service.

            schService = OpenService(schSCManager,        // SCM database
                                     SVCNAME,             // name of service
                                     SERVICE_ALL_ACCESS); // full access

            if (schService == IntPtr.Zero)
            {
                Console.WriteLine("OpenService failed " + Marshal.GetLastWin32Error());
                CloseServiceHandle(schSCManager);
                return;
            }

            // would be smart to check if running before starting

            if (!StartService(schService,  // handle to service
                              IntPtr.Zero, // number of arguments
                              null))       // no arguments
            {
                if (ERROR_SERVICE_ALREADY_RUNNING == Marshal.GetLastWin32Error())
                    Console.WriteLine("Service Already Running");
                else
                    Console.WriteLine("StartService failed " + Marshal.GetLastWin32Error());
            }

            //microsoft recommends adding a check here to see if 
            //the service started properly

            CloseServiceHandle(schService);
            CloseServiceHandle(schSCManager);
        }
    }
}
