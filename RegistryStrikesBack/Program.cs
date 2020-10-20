using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;

// This project partly ported from ideas in this PowerShell script:
// https://franckrichard.blogspot.com/2010/12/generate-reg-regedit-export-to-file.html

namespace RegistryStrikesBack
{
    class Program
    {
        public static string output = "Windows Registry Editor Version 5.00\r\n\r\n";
        public static string outputLocation = "Console";

        private static string transformStringReg(string input)
        {
            input = input.Replace("\\", "\\");
            input = input.Replace('"', '\"');
            return input;
        }

        private static string transformBinReg(byte[] input, int len, bool multi)
        {
            string outputLines = "";

            string[] hex_values_array = BitConverter.ToString(input).ToLower().Split('-');
            List<string> hex_values = new List<string>(hex_values_array);

            int totalValues = hex_values.Count;

            int diff = 25 - (int)Math.Ceiling(Decimal.Divide(len, 3));

            if (totalValues <= diff)
            {
                outputLines += String.Join(",", hex_values);
            }
            else
            {
                outputLines += String.Join(",", hex_values.Take(diff)) + ",\\\r\n";
                hex_values.RemoveRange(0, diff);
                while (hex_values.Count > 0)
                {
                    if (hex_values.Count > 25)
                    {
                        outputLines += String.Join(",", hex_values.Take(25)) + ",\\\r\n";
                        hex_values.RemoveRange(0, 25);
                    }
                    else
                    {
                        outputLines += String.Join(",", hex_values.Take(hex_values.Count));
                        hex_values.RemoveRange(0, hex_values.Count);
                    }
                }
            }
            return outputLines;
        }

        private static byte[] Combine(params byte[][] arrays)
        {
            byte[] rv = new byte[arrays.Sum(a => a.Length)];
            int offset = 0;
            foreach (byte[] array in arrays)
            {
                System.Buffer.BlockCopy(array, 0, rv, offset, array.Length);
                offset += array.Length;
            }
            return rv;
        }

        private static void processValueNames(RegistryKey Key)
        {
            output += "[" + Key.Name + "]\n";
            string[] valuenames = Key.GetValueNames();
            if (valuenames == null || valuenames.Length <= 0)
            {
                return;
            }
            foreach (string valuename in valuenames)
            {
                object obj = Key.GetValue(valuename);
                if (obj != null)
                {

                    string type = obj.GetType().Name;
                    RegistryValueKind valuekind = Key.GetValueKind(valuename);


                    int len = 0;
                    switch (valuekind.ToString())
                    {
                        case "String":
                            output += String.Format("\"{0}\"=\"{1}\"\r\n", transformStringReg(valuename), transformStringReg(obj.ToString()));
                            break;
                        case "ExpandString":
                            object tmpvalues = Key.GetValue(valuename, "error", RegistryValueOptions.DoNotExpandEnvironmentNames);
                            byte[] values = System.Text.Encoding.Unicode.GetBytes(tmpvalues.ToString() + "*");

                            len = String.Format("\"{0}\"=hex(2):", transformStringReg(valuename)).Length;
                            output += String.Format("\"{0}\"=hex(2):{1}\r\n", transformStringReg(valuename), transformBinReg(values, len, true));
                            break;
                        case "MultiString":
                            string[] stringvalues = (string[])obj;

                            byte[] totalbytes = new byte[0];
                            foreach (string stringvalue in stringvalues)
                            {
                                byte[] valuedata = System.Text.Encoding.Unicode.GetBytes(stringvalue + "*");
                                byte[] rv = new byte[totalbytes.Length + valuedata.Length];
                                System.Buffer.BlockCopy(totalbytes, 0, rv, 0, totalbytes.Length);
                                System.Buffer.BlockCopy(valuedata, 0, rv, totalbytes.Length, valuedata.Length);
                                totalbytes = rv;
                            }
                            len = String.Format("\"{0}\"=hex(7):", transformStringReg(valuename)).Length;
                            output += String.Format("\"{0}\"=hex(7):{1}\r\n", transformStringReg(valuename), transformBinReg(totalbytes, len, true));

                            break;
                        case "Binary":
                            len = String.Format("\"{0}\"=hex:", transformStringReg(valuename)).Length;
                            output += String.Format("\"{0}\"=hex:{1}\r\n", transformStringReg(valuename), transformBinReg((byte[])obj, len, false));
                            break;

                        case "DWord":
                            string value = String.Format("{0:x8}", (Int32)obj);
                            output += String.Format("\"{0}\"=dword:{1}\r\n", transformStringReg(valuename), value);
                            break;
                        case "QWord":
                            string qvalue = String.Format("{0:x16}", int.Parse(obj.ToString()));
                            output += String.Format("\"{0}\"=hex(b):{1}\r\n", transformStringReg(valuename), qvalue.Substring(14, 2) + "," + qvalue.Substring(12, 2) + "," + qvalue.Substring(10, 2) + "," + qvalue.Substring(8, 2) + "," + qvalue.Substring(6, 2) + "," + qvalue.Substring(4, 2) + "," + qvalue.Substring(2, 2) + "," + qvalue.Substring(0, 2));
                            break;
                        default:
                            break;
                    }

                    if (outputLocation == "Console")
                    {
                        Console.Write(output);
                    }
                    else
                    {
                        System.IO.File.AppendAllText(outputLocation, output);
                    }
                    output = "";
                }
            }
            output += "\r\n";
        }

        public static void OutputRegKey(RegistryKey Key)
        {
            try
            {
                string[] subkeynames = Key.GetSubKeyNames();
                if (subkeynames == null || subkeynames.Length <= 0)
                {
                    processValueNames(Key);
                    return;
                }
                foreach (string keyname in subkeynames)
                {
                    using (RegistryKey key2 = Key.OpenSubKey(keyname))
                        OutputRegKey(key2);
                }
                processValueNames(Key);
            }
            catch (Exception e)
            {
            }
        }
        static void Main(string[] args)
        {
            if (args.Length == 2)
            {
                outputLocation = args[1];
                Console.WriteLine("[*] Writing Output to {0}", outputLocation);
            }
            else if (args.Length == 1)
            {
                outputLocation = "Console";
            }
            else
            {
                Console.WriteLine("RegistryStrikesBack.exe <key> [output file path]");
                return;
            }

            string key = args[0];

            RegistryKey reg = Registry.CurrentUser;
            if (key.StartsWith("HKCU\\"))
            {
                key = key.Replace("HKCU\\", "");
            }
            else if (key.StartsWith("HKLM\\"))
            {
                reg = Registry.LocalMachine;
                key = key.Replace("HKLM\\", "");
            }
            else if (key.StartsWith("HKCR\\"))
            {
                reg = Registry.ClassesRoot;
                key = key.Replace("HKCR\\", "");
            }
            else if (key.StartsWith("HKU\\"))
            {
                reg = Registry.Users;
                key = key.Replace("HKU\\", "");
            }
            else if (key.StartsWith("HKCC\\"))
            {
                reg = Registry.CurrentConfig;
                key = key.Replace("HKCC\\", "");
            }

            RegistryKey RegKey = reg.OpenSubKey(key);
            OutputRegKey(RegKey);
        }
    }
}
