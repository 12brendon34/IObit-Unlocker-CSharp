using System;
using System.IO;

namespace IObit_Unlocker_CSharp
{
    internal class Program
    {
        //if winforms project CreateDriverInstance(); shuld be ran on startup
        //ie
        /*
         
        public Form1()
        {
            InitializeComponent();

            //installs enables and starts the driver
                                
            
            //if (!File.Exists("IObitUnlocker.sys"))            here <------------------|
            //{                                                                |
            //    Console.WriteLine("IObitUnlocker.sys does not exist.");      |
            //    return;                                                      |
            //}                                                                |
                                                                               |
            Iobit.SvcInstall(); // added a check for if the driver sys exists  |
                                // on line 274, you can remove the check if    |
                                // you want the check somewhere else ie     ---|
            Iobit.EnableSvc();
            Iobit.StartSvc();

            //allocates buffer's for driver inputs
            Iobit.CreateDriverInstance();
        }
        
        //or on your custom close button click
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            Iobit.DisableSvc();
        } 

         */
        static void Main(string[] args)
        {
            string DriverPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "IObitUnlocker.sys"
                );

            IObitUnlocker.SvcInstall(DriverPath);
            IObitUnlocker.EnableSvc();
            IObitUnlocker.StartSvc();

            Console.WriteLine("Select Operation");
            Console.WriteLine("1 - Delete, 2 - Move, or 3 - Close");

            var option = Convert.ToInt32(Console.ReadLine());
            Console.WriteLine("Don't Use Quotation marks");
            switch (option)
            {
                case 1:
                    Console.WriteLine("Enter path to delete");
                    IObitUnlocker.Delete(Console.ReadLine());
                    break;
                case 2:
                    Console.WriteLine("enter original path");
                    string path1 = Console.ReadLine();

                    Console.WriteLine("enter path to move to");
                    string path2 = Console.ReadLine();

                    IObitUnlocker.Move(path1, path2);
                    break;
                case 3:
                    System.Environment.Exit(0);
                    break;
            }

            IObitUnlocker.DisableSvc();

            Console.WriteLine("Press enter to quit");
            Console.ReadLine();
        }
    }
}
