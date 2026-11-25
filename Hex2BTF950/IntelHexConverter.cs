using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Hex2BTF950
{
    internal static class IntelHexConverter
    {
        public static byte[] ConvertHexToBin(string hexFilePath)
        {
            var memory = new SortedDictionary<int, byte>();
            int upperAddress = 0;

            foreach (var line in File.ReadLines(hexFilePath))
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (!line.StartsWith(':'))
                    throw new InvalidDataException($"Invalid HEX line: {line}");

                int byteCount = int.Parse(line.Substring(1, 2), NumberStyles.HexNumber);
                int address = int.Parse(line.Substring(3, 4), NumberStyles.HexNumber);
                int recordType = int.Parse(line.Substring(7, 2), NumberStyles.HexNumber);
                string dataField = line.Substring(9, byteCount * 2);
                int checksum = int.Parse(line.Substring(9 + byteCount * 2, 2), NumberStyles.HexNumber);

                switch (recordType)
                {
                    case 0x00: // Data Record
                        int absoluteAddress = upperAddress + address;
                        for (int i = 0; i < byteCount; i++)
                        {
                            byte dataByte = byte.Parse(dataField.Substring(i * 2, 2), NumberStyles.HexNumber);
                            memory[absoluteAddress + i] = dataByte;
                        }
                        break;

                    case 0x01: // End Of File
                        goto EndOfFile;

                    case 0x02: // Extended Segment Address Record
                        upperAddress = int.Parse(dataField, NumberStyles.HexNumber) << 4;
                        break;

                    case 0x04: // Extended Linear Address Record
                        upperAddress = int.Parse(dataField, NumberStyles.HexNumber) << 16;
                        break;

                    default:
                        // Other record types (0x03, 0x05) are rarely used, can be ignored or logged
                        break;
                }
            }

        EndOfFile:

            if (memory.Count == 0)
                throw new InvalidDataException("No data found in HEX file.");

            // Find min and max addresses
            int minAddress = int.MaxValue;
            int maxAddress = int.MinValue;

            foreach (int addr in memory.Keys)
            {
                if (addr < minAddress) minAddress = addr;
                if (addr > maxAddress) maxAddress = addr;
            }

            int length = maxAddress - minAddress + 1;
            byte[] binData = new byte[length];

            for (int i = 0; i < length; i++)
            {
                binData[i] = memory.ContainsKey(minAddress + i) ? memory[minAddress + i] : (byte)0xFF; // Fill gaps with 0xFF
            }

            return binData;
        }
    }
}
