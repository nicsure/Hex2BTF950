using System;
using System.Collections.Generic;
using System.Text;

namespace Hex2BTF950
{
    public static class Hex2BTF950_Main
    {

        private static void Log(string message)
        {
            Console.WriteLine(message);
        }

        private static void Err(string message)
        {
            Console.Error.WriteLine(message);
        }


        public static void Go(string[] args)
        {
            // brief instructions for the user if no arguments are provided
            if (args.Length == 0)
            {
                Log("Provide path to a valid Intel HEX file.");
                return;
            }

            // assume first argument is the path to the Intel HEX file
            string hexfile = args[0];

            // read the Intel HEX file and convert it to binary
            // handle any exceptions that may occur during the conversion
            byte[] data;
            try
            {
                data = IntelHexConverter.ConvertHexToBin(hexfile);
            }
            catch (Exception ex)
            {
                Err(ex.Message);
                return;
            }

            // check if the data is zero length and log an error if it is
            if (data.Length == 0)
            {
                Err("Zero length binary");
                return;
            }

            // BTF file requires padding to a 1024 byte boundary
            int len = 1024 + ((int)Math.Ceiling(data.Length / 1024.0) * 1024);

            // create a new byte array with the padded length
            byte[] btf = new byte[len];

            // copy the first 1024 bytes of data to the beginning of the new array
            // this is "unencryted" data, but we need to handle the edge case of the binary being less than 1024 bytes
            Array.Copy(data, 0, btf, 0, data.Length <= 1024 ? data.Length : 1024);

            // now we copy the rest of the data starting at index 1024 to the new array, but this starts at 2048 in the new array
            // because index 1024 to 2047 of the firmware BTF file is the decryption key
            // this leave the decryption key area as all zeros i.e. no encryption, meaning we can simply copy the rest of the binary data
            // without having to worry about encryption at all.
            if (data.Length > 1024)
                Array.Copy(data, 1024, btf, 2048, data.Length - 1024);

            // if a second argument is provided, use that as the destination path
            // otherwise, use the same path & filename as the hex file but change the extension to .btf
            string dest;
            if (args.Length > 1)
            {
                dest = args[1];
            }
            else
            {
                string s = Path.GetFileNameWithoutExtension(hexfile);
                string d = Path.GetDirectoryName(hexfile) ?? string.Empty;
                dest = Path.Combine(d, $"{s}.btf");
            }

            // write the new byte array to the destination btf file
            // handle any exceptions that may occur during the file write operation
            try
            {
                File.WriteAllBytes(dest, btf);
            }
            catch (Exception ex)
            {
                Err(ex.Message);
                return;
            }
            Log($"Created binary: {dest}");
        }

    }
}
