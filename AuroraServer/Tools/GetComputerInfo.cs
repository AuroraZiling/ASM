using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace AuroraServer.Tools
{
    class GetComputerInfo
    {
        public static string GetCpuInfo()
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(@"root\CIMV2", "SELECT * FROM Win32_Processor");
            foreach (ManagementObject obj2 in searcher.Get())
            {
                try
                {
                    return (obj2.GetPropertyValue("Name").ToString());
                }
                catch
                {
                    continue;
                }
            }
            return "未知";
        }
        public static string GetMemoryInfo()
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher();   //用于查询一些如系统信息的管理对象 
            searcher.Query = new SelectQuery("Win32_PhysicalMemory ", "", new string[] { "Capacity" });//设置查询条件 
            ManagementObjectCollection collection = searcher.Get();   //获取内存容量 
            ManagementObjectCollection.ManagementObjectEnumerator em = collection.GetEnumerator();

            long capacity = 0;
            while (em.MoveNext())
            {
                ManagementBaseObject baseObj = em.Current;
                if (baseObj.Properties["Capacity"].Value != null)
                {
                    try
                    {
                        capacity += (long.Parse(baseObj.Properties["Capacity"].Value.ToString())) / 1024 / 1024 / 1024;
                    }
                    catch
                    {
                        capacity = 0;
                    }
                }
            }

            searcher.Query = new SelectQuery("Win32_PhysicalMemory ", "", new string[] { "Speed" });//设置查询条件 
            collection = searcher.Get();   //获取内存速度（即频率） 
            em = collection.GetEnumerator();

            string speed = "";
            while (em.MoveNext())
            {
                ManagementBaseObject baseObj = em.Current;
                if (baseObj.Properties["Speed"].Value != null)
                {
                    try
                    {
                        speed = baseObj.Properties["Speed"].Value.ToString();
                    }
                    catch
                    {
                        speed = "";
                    }
                }
            }
            string mi = "";
            if (capacity > 0 && speed.Length > 0)
            {
                mi = capacity.ToString() + " GB, " + speed + " MHz";
            }
            return mi;
        }
    }
}
