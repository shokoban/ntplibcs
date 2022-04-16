using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NtpLibCs
{
    /// <summary>
    /// Exception raised by this module.
    /// </summary>
    public class NtpException : Exception
    {
        public NtpException(string message = "")
        {
            throw new NotImplementedException(message);
        }
    }

    /// <summary>
    /// Helper class defining constants.
    /// </summary>
    public static class Ntp
    {
        /// <summary>
        /// system epoch
        /// </summary>
        public static readonly DateTimeOffset SystemEpoch =
            new DateTimeOffset(new DateTime(1970, 1, 1), new TimeSpan(0, 0, 0));

        /// <summary>
        /// NTP epoch
        /// </summary>
        public static readonly DateTimeOffset NtpEpoch =
            new DateTimeOffset(new DateTime(1900, 1, 1), new TimeSpan(0, 0, 0));

        /// <summary>
        /// delta between system and NTP time. Unit is second.
        /// </summary>
        // Assigning an expression using a static variable to
        // a static variable will result in a value of 0 at runtime,
        // so assign the value directly.
        // public static readonly TimeSpan NtpDelta = SystemEpoch - NtpEpoch;
        public const double NtpDelta = 2208988800;

        /// <summary>
        /// reference identifier table 
        /// </summary>
        public static readonly Dictionary<string, string> RefIdTable = new Dictionary<string, string>()
        {
            {"GOES", "Geostationary Orbit Environment Satellite"},
            {"GPS\0", "Global Position System"},
            {"GAL\0", "Galileo Positioning System"},
            {"PPS\0", "Generic pulse-per-second"},
            {"IRIG", "Inter-Range Instrumentation Group"},
            {"WWVB", "LF Radio WWVB Ft. Collins, CO 60 kHz"},
            {"DCF\0", "LF Radio DCF77 Mainflingen, DE 77.5 kHz"},
            {"HBG\0", "LF Radio HBG Prangins, HB 75 kHz"},
            {"MSF\0", "LF Radio MSF Anthorn, UK 60 kHz"},
            {"JJY\0", "LF Radio JJY Fukushima, JP 40 kHz, Saga, JP 60 kHz"},
            {"LORC", "MF Radio LORAN C station, 100 kHz"},
            {"TDF\0", "MF Radio Allouis, FR 162 kHz"},
            {"CHU\0", "HF Radio CHU Ottawa, Ontario"},
            {"WWV\0", "HF Radio WWV Ft. Collins, CO"},
            {"WWVH", "HF Radio WWVH Kauai, HI"},
            {"NIST", "NIST telephone modem"},
            {"ACTS", "NIST telephone modem"},
            {"USNO", "USNO telephone modem"},
            {"PTB\0", "European telephone modem"},
            {"LOCL", "uncalibrated local clock"},
            {"CESM", "calibrated Cesium clock"},
            {"RBDM", "calibrated Rubidium clock"},
            {"OMEG", "OMEGA radionavigation system"},
            {"DCN\0", "DCN routing protocol"},
            {"TSP\0", "TSP time protocol"},
            {"DTS\0", "Digital Time Service"},
            {"ATOM", "Atomic clock (calibrated)"},
            {"VLF\0", "VLF radio (OMEGA,, etc.)"},
            {"1PPS", "External 1 PPS input"},
            {"FREE", "(Internal clock)"},
            {"INIT", "(Initialization)"},
            {"ROA\0", "Real Observatorio de la Armada"},
            {"\0\0\0\0", "NULL"},
        };

        /// <summary>
        /// stratum table
        /// </summary>
        public static readonly Dictionary<int, string> StratumTable = new Dictionary<int, string>()
        {
            {0, "unspecified or invalid"},
            {1, "primary reference"},
        };

        /// <summary>
        /// mode table
        /// </summary>
        public static readonly Dictionary<int, string> ModeTable = new Dictionary<int, string>()
        {
            {0, "reserved"},
            {1, "symmetric active"},
            {2, "symmetric passive"},
            {3, "client"},
            {4, "server"},
            {5, "broadcast"},
            {6, "reserved for NTP control messages"},
            {7, "reserved for private use"},
        };

        /// <summary>
        /// leap indicator table
        /// </summary>
        public static readonly Dictionary<int, string> LeapTable = new Dictionary<int, string>()
        {
            {0, "no warning"},
            {1, "last minute of the day has 61 seconds"},
            {2, "last minute of the day has 59 seconds"},
            {3, "unknown (clock unsynchronized)"},
        };
    }

    /// <summary>
    /// NTP packet class.
    /// This represents an NTP packet. 
    /// </summary>
    public class NtpPacket
    {
        /// <summary>
        /// leap second indicator
        /// </summary>
        public byte Leap;

        /// <summary>
        /// version
        /// </summary>
        public byte Version;

        /// <summary>
        /// mode
        /// </summary>
        public byte Mode;

        /// <summary>
        /// stratum
        /// </summary>
        public byte Stratum;

        /// <summary>
        /// poll interval
        /// </summary>
        public sbyte Poll;

        /// <summary>
        /// precision
        /// </summary>
        public sbyte Precision;

        /// <summary>
        /// root delay
        /// </summary>
        public double RootDelay;

        /// <summary>
        /// root dispersion
        /// </summary>
        public double RootDispersion;

        /// <summary>
        /// reference clock identifier
        /// </summary>
        public uint RefId;

        /// <summary>
        /// reference timestamp
        /// </summary>
        public double RefTimestamp;

        /// <summary>
        /// originate timestamp
        /// </summary>
        public double OrigTimestamp;

        /// <summary>
        /// receive timestamp
        /// </summary>
        public double RecvTimestamp;

        /// <summary>
        /// transmit timestamp
        /// </summary>
        public double TxTimestamp;

        public NtpPacket(byte version = 3, byte mode = 3, double txTimestamp = 0)
        {
            Leap = 0;
            Version = version;
            Mode = mode;
            Stratum = 0;
            Poll = 0;
            Precision = 0;
            RootDelay = 0;
            RootDispersion = 0;
            RefId = 0;
            RefTimestamp = 0;
            OrigTimestamp = 0;
            RecvTimestamp = 0;
            TxTimestamp = txTimestamp;
        }

        /// <summary>
        /// Convert this NTPPacket to a buffer that can be sent over a socket.
        /// </summary>
        /// <returns>buffer representing this packet</returns>
        /// <exception cref="NtpException">in case of invalid field</exception>
        public byte[] ToData()
        {
            var packed = new byte[48];
            try
            {
                packed[0] = (byte) ((Leap << 6) | (Version << 3) | (Mode));
                packed[1] = Stratum;
                packed[2] = (byte) Poll;
                packed[3] = (byte) Precision;
                ToBytes(NtpUtils.ToIntPart(RootDelay) << 16 | NtpUtils.ToFracPart(RootDelay, 16))
                    .CopyTo(packed, 4);
                ToBytes(NtpUtils.ToIntPart(RootDispersion) << 16 | NtpUtils.ToFracPart(RootDispersion, 16))
                    .CopyTo(packed, 8);
                ToBytes(RefId).CopyTo(packed, 12);
                ToBytes(NtpUtils.ToIntPart(RefTimestamp)).CopyTo(packed, 16);
                ToBytes(NtpUtils.ToFracPart(RefTimestamp)).CopyTo(packed, 20);
                ToBytes(NtpUtils.ToIntPart(OrigTimestamp)).CopyTo(packed, 24);
                ToBytes(NtpUtils.ToFracPart(OrigTimestamp)).CopyTo(packed, 28);
                ToBytes(NtpUtils.ToIntPart(RecvTimestamp)).CopyTo(packed, 32);
                ToBytes(NtpUtils.ToFracPart(RecvTimestamp)).CopyTo(packed, 36);
                ToBytes(NtpUtils.ToIntPart(TxTimestamp)).CopyTo(packed, 40);
                ToBytes(NtpUtils.ToFracPart(TxTimestamp)).CopyTo(packed, 44);
            }
            catch (Exception e)
            {
                throw new NtpException(e.Message);
            }

            return packed;
        }

        /// <summary>
        /// Converts a value to a big-endian byte array
        /// </summary>
        /// <param name="v">Value</param>
        /// <returns>big-endian byte array</returns>
        private byte[] ToBytes(uint v)
        {
            var bytes = BitConverter.GetBytes(v);
            if (BitConverter.IsLittleEndian)
                bytes = bytes.Reverse().ToArray();
            return (byte[]) bytes.Clone();
        }

        /// <summary>
        /// Populate this instance from a NTP packet payload received from the network.
        /// </summary>
        /// <param name="data">buffer payload</param>
        public void FromData(byte[] data)
        {
            Leap = (byte) (data[0] >> 6 & 0b11);
            Version = (byte) (data[0] >> 3 & 0b111);
            Mode = (byte) (data[0] & 0b111);
            Stratum = data[1];
            Poll = (sbyte) data[2];
            Precision = (sbyte) data[3];
            RootDelay = FromBytes(data, 4) / Math.Pow(2, 16);
            RootDispersion = FromBytes(data, 8) / Math.Pow(2, 16);
            RefId = FromBytes(data, 12);
            RefTimestamp = NtpUtils.ToTime(FromBytes(data, 16), FromBytes(data, 20));
            OrigTimestamp = NtpUtils.ToTime(FromBytes(data, 24), FromBytes(data, 28));
            RecvTimestamp = NtpUtils.ToTime(FromBytes(data, 32), FromBytes(data, 36));
            TxTimestamp = NtpUtils.ToTime(FromBytes(data, 40), FromBytes(data, 44));
        }

        /// <summary>
        /// Convert a big-endian byte array to a value
        /// </summary>
        /// <param name="data">buffer payload</param>
        /// <param name="start">start index of copying</param>
        /// <returns></returns>
        private uint FromBytes(byte[] data, uint start)
        {
            var bytes = new byte[4];
            Array.Copy(data, start, bytes, 0, 4);
            if (BitConverter.IsLittleEndian)
                bytes = bytes.Reverse().ToArray();
            return BitConverter.ToUInt32(bytes, 0);
        }
    }

    /// <summary>
    /// NTP statistics.
    /// Wrapper for NTPPacket, offering additional statistics like offset and delay, and timestamps converted to system time.
    /// </summary>
    public class NtpStats : NtpPacket
    {
        /// <summary>
        /// destination timestamp
        /// </summary>
        public double DestTimestamp { get; set; } = 0;

        /// <summary>
        /// Offset
        /// </summary>
        public double Offset => ((RecvTimestamp - OrigTimestamp) + (TxTimestamp - DestTimestamp)) / 2;

        /// <summary>
        /// round-trip delay
        /// </summary>
        public double Delay => ((DestTimestamp - OrigTimestamp) - (TxTimestamp - RecvTimestamp));

        /// <summary>
        /// Transmit timestamp in system time.
        /// </summary>
        public double TxTime => NtpUtils.NtpToSystemTime(TxTimestamp);

        /// <summary>
        /// Receive timestamp in system time.
        /// </summary>
        public double RecvTime => NtpUtils.NtpToSystemTime(RecvTimestamp);

        /// <summary>
        /// Originate timestamp in system time.
        /// </summary>
        public double OrigTime => NtpUtils.NtpToSystemTime(OrigTimestamp);

        /// <summary>
        /// Reference timestamp in system time.
        /// </summary>
        public double RefTime => NtpUtils.NtpToSystemTime(RefTimestamp);

        /// <summary>
        /// Destination timestamp in system time.
        /// </summary>
        public double DestTime => NtpUtils.NtpToSystemTime(DestTimestamp);
    }

    /// <summary>
    /// NTP client session.
    /// </summary>
    public class NtpClient
    {
        /// <summary>
        /// Query a NTP server.
        /// </summary>
        /// <param name="host">server name/address</param>
        /// <param name="version">NTP version to use</param>
        /// <param name="port">server port</param>
        /// <param name="timeout">timeout on socket operations</param>
        /// <returns>NTPStats object</returns>
        public NtpStats Request(string host, byte version = 2, int port = 123, int timeout = 5)
        {
            // lookup server address
            var address = Dns.GetHostAddresses(host)
                .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
            if (address is null)
                throw new NtpException("Could not get the corresponding address from the host name..");

            var ipEndPoint = new IPEndPoint(address, port);

            // create the socket
            var socket = new UdpClient();

            byte[] responsePacket;
            double destTimestamp;
            try
            {
                socket.Connect(ipEndPoint);
                socket.Client.SendTimeout = timeout * 1000;
                socket.Client.ReceiveTimeout = timeout * 1000;

                // create the request packet - mode 3 is client
                var queryPacket = new NtpPacket(
                    mode: 3,
                    version: version,
                    txTimestamp: ((DateTimeOffset.Now - Ntp.NtpEpoch).Ticks / 10000000d)
                );

                // send the request
                var packetBytes = queryPacket.ToData();
                socket.Send(packetBytes, packetBytes.Length);

                // wait for the response
                responsePacket = socket.Receive(ref ipEndPoint);

                // build the destination timestamp
                destTimestamp = ((DateTimeOffset.Now - Ntp.NtpEpoch).Ticks / 10000000d);
            }
            catch (SocketException e)
            {
                throw new NtpException($"No response received from {host}. \nMessage: {e.Message}");
            }
            finally
            {
                socket.Close();
            }

            // construct corresponding statistics
            var state = new NtpStats();
            state.FromData(responsePacket);
            state.DestTimestamp = destTimestamp;

            return state;
        }
    }

    public static class NtpUtils
    {
        /// <summary>
        /// Return the integral part of a timestamp.
        /// </summary>
        /// <param name="timestamp">NTP timestamp</param>
        /// <returns>integral part</returns>
        public static uint ToIntPart(double timestamp)
        {
            return Convert.ToUInt32(Math.Truncate(timestamp));
        }

        /// <summary>
        /// Return the fractional part of a timestamp.
        /// </summary>
        /// <param name="timestamp">NTP timestamp</param>
        /// <param name="n">number of bits of the fractional part</param>
        /// <returns>fractional part</returns>
        public static uint ToFracPart(double timestamp, int n = 32)
        {
            return Convert.ToUInt32(Math.Truncate(Math.Abs(timestamp - ToIntPart(timestamp)) * Math.Pow(2, n)));
        }

        /// <summary>
        /// Return a timestamp from an integral and fractional part.
        /// </summary>
        /// <param name="intPart">integral part</param>
        /// <param name="fracPart">fractional part</param>
        /// <param name="n">number of bits of the fractional part</param>
        /// <returns>timestamp</returns>
        public static double ToTime(uint intPart, uint fracPart, int n = 32)
        {
            return intPart + Convert.ToDouble(fracPart) / Math.Pow(2, n);
        }

        /// <summary>
        /// Convert a NTP time to system time.
        /// </summary>
        /// <param name="timestamp">timestamp in NTP time</param>
        /// <returns>corresponding system time</returns>
        public static double NtpToSystemTime(double timestamp)
        {
            return timestamp - Ntp.NtpDelta;
        }

        /// <summary>
        /// Convert a system time to a NTP time.
        /// </summary>
        /// <param name="timestamp">timestamp in system time</param>
        /// <returns>corresponding NTP time</returns>
        public static double SystemToNtpTime(double timestamp)
        {
            return timestamp + Ntp.NtpDelta;
        }

        /// <summary>
        /// Convert a leap indicator to text.
        /// </summary>
        /// <param name="leap">leap indicator value</param>
        /// <returns>corresponding message</returns>
        /// <exception cref="NtpException">in case of invalid leap indicator</exception>
        public static string LeapToText(byte leap)
        {
            if (Ntp.LeapTable.ContainsKey(leap) == false)
                throw new NtpException("Invalid leap indicator.");
            return Ntp.LeapTable[leap];
        }

        /// <summary>
        /// Convert a NTP mode value to text.
        /// </summary>
        /// <param name="mode">NTP mode</param>
        /// <returns>corresponding message</returns>
        /// <exception cref="NtpException">in case of invalid mode</exception>
        public static string ModeToText(byte mode)
        {
            if (Ntp.ModeTable.ContainsKey(mode) == false)
                throw new NtpException("Invalid mode.");
            return Ntp.ModeTable[mode];
        }

        /// <summary>
        /// Convert a stratum value to text.
        /// </summary>
        /// <param name="stratum">NTP stratum</param>
        /// <returns>corresponding message</returns>
        /// <exception cref="NtpException">in case of invalid stratum</exception>
        public static string StratumToText(byte stratum)
        {
            if (Ntp.StratumTable.ContainsKey(stratum))
                return $"{Ntp.StratumTable[stratum]} ({stratum})";

            if (1 < stratum && stratum < 16)
                return $"secondary reference ({stratum})";
            if (stratum == 16)
                return $"unsynchronized ({stratum})";

            throw new NtpException("Invalid stratum or reserved.");
        }

        /// <summary>
        /// Convert a reference clock identifier to text according to its stratum.
        /// </summary>
        /// <param name="refId">reference clock indentifier</param>
        /// <param name="stratum">NTP stratum</param>
        /// <returns>corresponding message</returns>
        /// <exception cref="NtpException">in case of invalid stratum</exception>
        public static string RefIdToText(uint refId, byte stratum = 2)
        {
            var fields = new[]
            {
                (byte) (refId >> 24 & 0xff),
                (byte) (refId >> 16 & 0xff),
                (byte) (refId >> 8 & 0xff),
                (byte) (refId >> 0 & 0xff)
            };

            if (stratum <= 1)
            {
                var text = Encoding.UTF8.GetString(fields);
                if (Ntp.RefIdTable.ContainsKey(text) == false)
                    return $"Unidentified reference source '{text}'";

                return Ntp.RefIdTable[text];
            }

            if (stratum < 255)
                return $"{fields[0]}.{fields[1]}.{fields[2]}.{fields[3]}";

            throw new NtpException("Invalid stratum.");
        }
    }
}
