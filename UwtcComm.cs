using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO.Ports;
using NUnit.Framework;

namespace TCore
{
    public partial class UwtcComm
    {
        public delegate void LogSzDel(object crid, string s);

        private LogSzDel m_log = null;

        public UwtcComm()
        {
            m_log = null;
        }

        public UwtcComm(LogSzDel log)
        {
            m_log = log;
        }

        void LogSz(object crid, string s)
        {
            if (m_log != null)
                m_log(crid, s);
        }

        public class UwtcSerialPort
        {
            private SerialPort m_ser;
            private SerialPortMock m_serm;

            public UwtcSerialPort(SerialPort ser)
            {
                m_ser = ser;
            }

            public UwtcSerialPort(SerialPortMock serm)
            {
                m_serm = serm;
            }

            public void Open()
            {
                if (m_ser != null)
                    m_ser.Open();
                else
                    m_serm.Open();
            }

            public void Close()
            {
                if (m_ser != null)
                    m_ser.Close();
                else
                    m_serm.Close();
            }

            public int ReadByte()
            {
                if (m_ser != null)
                    return m_ser.ReadByte();

                return m_serm.ReadByte();
            }

            public Int64 ReadingIndex { get { return m_serm == null ? 0 : m_serm.StepIndex; } }
            public SerialPort RawPort { get { return m_ser; } }
        }

        public partial class UwtcPacket
        {
            private byte[] m_rgbPacketRaw;
            private bool m_fParsed;
            private UInt16 m_cbPacket;
            private byte m_bApiID;
            private UInt16 m_wAddress;
            private byte m_bSignalStrength;
            private byte m_bReserved;
            private byte m_bSensorType;
            private UInt16 m_nProcessData;
            // private Single m_flProcessData;
            private Single m_flAmbientTemp;
            private UInt16 m_wBattery;
            private byte m_bChecksum;

            public byte[] PacketRaw { get { return m_rgbPacketRaw; } private set { m_rgbPacketRaw = value; } }
            public bool Parsed { get { return m_fParsed; } private set { m_fParsed = value; } }
            public UInt16 PacketLength { get { return m_cbPacket; } private set { m_cbPacket = value; } }
            public byte ApiID { get { return m_bApiID; } private set { m_bApiID = value; } }
            public UInt16 Address { get { return m_wAddress; } private set { m_wAddress = value; } }
            public byte SignalStrength { get { return m_bSignalStrength; } private set { m_bSignalStrength = value; } }
            public byte Reserved { get { return m_bReserved; } private set { m_bReserved = value; } }
            public byte SensorType { get { return m_bSensorType; } private set { m_bSensorType = value; } }
            public UInt16 ProcessData { get { return m_nProcessData; } private set { m_nProcessData = value; } }
//            public Single FlProcessData { get { return m_flProcessData; } private set { m_flProcessData = value; } }
            public Single AmbientTemp { get { return m_flAmbientTemp; } private set { m_flAmbientTemp = value; } }
            public UInt16 Battery { get { return m_wBattery; } private set { m_wBattery = value; } }
            public byte Checksum { get { return m_bChecksum; } private set { m_bChecksum = value; } }

            private byte m_ib;

            public UwtcPacket()
            {
                m_rgbPacketRaw = new byte[18];
            }

            /* C O M B I N E  S T R I N G  B Y T E S  T O  N U M B E R */
            /*----------------------------------------------------------------------------
            	%%Function: CombineStringBytesToNumber
            	%%Qualified: TCore.UwtcComm:UwtcPacket.CombineStringBytesToNumber
            	%%Contact: rlittle

            ----------------------------------------------------------------------------*/
            public static UInt16 CombineStringBytesToNumber(string s1, string s2)
            {
                byte bLow, bHigh;

                bHigh = Convert.ToByte(s1, 16);
                bLow = Convert.ToByte(s2, 16);

                return CombineBytesToNumber(bHigh, bLow);
            }

            /* C O M B I N E  B Y T E S  T O  N U M B E R */
            /*----------------------------------------------------------------------------
            	%%Function: CombineBytesToNumber
            	%%Qualified: TCore.UwtcComm:UwtcPacket.CombineBytesToNumber
            	%%Contact: rlittle

            ----------------------------------------------------------------------------*/
            public static UInt16 CombineBytesToNumber(byte bHigh, byte bLow)
            {
                return (UInt16) ((bHigh * 256) + bLow);
            }

            /* F  P A R S E  P R O L O G U E */
            /*----------------------------------------------------------------------------
            	%%Function: FParsePrologue
            	%%Contact: rlittle

            ----------------------------------------------------------------------------*/
            private static bool FParsePrologue(byte[] rgb, int cb)
            {
                byte bPrologue;

                if (!FParseGenericByte(rgb, cb, s_ibPacketPrologue, out bPrologue))
                    return false;

                if (bPrologue != 0x7e)
                    return false;

                return true;
            }

            /* F  P A R S E  L E N G T H */
            /*----------------------------------------------------------------------------
            	%%Function: FParseLength
            	%%Qualified: TCore.FParseLength
            	%%Contact: rlittle

            ----------------------------------------------------------------------------*/
            private static bool FParseLength(byte[] rgb, int cb, out UInt16 wLength)
            {
                return FParseGenericWord(rgb, cb, s_ibLengthLow, s_ibLengthHigh, out wLength);
            }

            // NOTE that the documentation in M-4620-A.pdf for the uwtc-rec protocol is WRONG
            // about the byte ordering (low / high) for several of the WORD fields.  ALL of the
            // WORD fields appear to be High Byte, Low Byte (MSB, LSB).
            public const int s_ibPacketPrologue = 0;
            public const int s_ibLengthHigh = 1;
            public const int s_ibLengthLow = 2;
            public const int s_ibApiID = 3;
            public const int s_ibAddressHigh = 4;
            public const int s_ibAddressLow = 5;
            public const int s_ibSignalStrength = 6;
            public const int s_ibReserved = 7;
            public const int s_ibSensorType = 8;
            public const int s_ibProcessDataHigh = 9;
            public const int s_ibProcessDataLow = 10;
            public const int s_ibAmbientHigh = 11;
            public const int s_ibAmbientLow = 12;
            public const int s_ibBatteryHigh = 13;
            public const int s_ibBatteryLow = 14;
            public const int s_ibChecksum = 15;

            private const int s_cbPacketData = s_cbPacketFull - 4; // remove start of Frame, Length Bytes, and checksum
            private const int s_cbPacketFull = 16;

            private static bool FParseGenericByte(byte []rgb, int cb, int ib, out byte b)
            {
                b = 0;
                if (cb <= ib)
                    return false;

                b = rgb[ib];
                return true;
            }

            private static bool FParseApiID(byte []rgb, int cb, out byte bApiID)
            {
                return FParseGenericByte(rgb, cb, s_ibApiID, out bApiID);
            }

            private static bool FParseSignalStrength(byte []rgb, int cb, out byte b)
            {
                return FParseGenericByte(rgb, cb, s_ibSignalStrength, out b);
            }

            private static bool FParseReserved(byte []rgb, int cb, out byte b)
            {
                return FParseGenericByte(rgb, cb, s_ibReserved, out b);
            }

            private static bool FParseSensorType(byte []rgb, int cb, out byte b)
            {
                return FParseGenericByte(rgb, cb, s_ibSensorType, out b);
            }

            private static bool FParseChecksum(byte []rgb, int cb, out byte b)
            {
                return FParseGenericByte(rgb, cb, s_ibChecksum, out b);
            }

            private static bool FParseGenericWord(byte []rgb, int cb, int ibLow, int ibHigh, out UInt16 w)
            {
                w = 0;
                if (cb <= Math.Max(ibHigh, ibLow))
                    return false;

                w = CombineBytesToNumber(rgb[ibHigh], rgb[ibLow]);
                return true;
            }

            private static bool FParseAddress(byte []rgb, int cb, out UInt16 w)
            {
                return FParseGenericWord(rgb, cb, s_ibAddressLow, s_ibAddressHigh, out w);
            }

            private static bool FParseProcessData(byte []rgb, int cb, out UInt16 w)
            {
                return FParseGenericWord(rgb, cb, s_ibProcessDataLow, s_ibProcessDataHigh, out w);
            }

            private static bool FParseAmbient(byte []rgb, int cb, out UInt16 w)
            {
                return FParseGenericWord(rgb, cb, s_ibAmbientLow, s_ibAmbientHigh, out w);
            }

            private static bool FParseBattery(byte []rgb, int cb, out UInt16 w)
            {
                return FParseGenericWord(rgb, cb, s_ibBatteryLow, s_ibBatteryHigh, out w);
            }

            /* P A R S E  R G B */
            /*----------------------------------------------------------------------------
            	%%Function: ParseRgb
            	%%Qualified: TCore.UwtcComm:UwtcPacket.ParseRgb
            	%%Contact: rlittle
             
                parse the given array of bytes into the packet
            ----------------------------------------------------------------------------*/
            public static bool ParseRgb(UwtcPacket uwp, byte[] rgb, int cb)
            {
                if (!FParsePrologue(rgb, cb))
                    return false;

                if (cb != s_cbPacketFull)
                    return false;

                UInt16 w;
                byte b;

                if (!FParseLength(rgb, cb, out w))
                    return false;

                uwp.PacketLength = w;

                if (w != s_cbPacketData)
                    return false;

                if (!FParseApiID(rgb, cb, out b))
                    return false;

                uwp.ApiID = b;

                if (!FParseAddress(rgb, cb, out w))
                    return false;

                uwp.Address = w;

                if (!FParseSignalStrength(rgb, cb, out b))
                    return false;

                uwp.SignalStrength = b;

                if (!FParseReserved(rgb, cb, out b))
                    return false;

                uwp.Reserved = b;

                if (!FParseSensorType(rgb, cb, out b))
                    return false;

                uwp.SensorType = b;

                if (!FParseProcessData(rgb, cb, out w))
                    return false;
                
                uwp.ProcessData = w;

                if (!FParseAmbient(rgb, cb, out w))
                    return false;

                uwp.AmbientTemp = ((float)w) / 10.0f;

                if (!FParseBattery(rgb, cb, out w))
                    return false;

                uwp.Battery = w;

                if (!FParseChecksum(rgb, cb, out b))
                    return false;

                uwp.Checksum = b;

                uwp.Parsed = true;
                return true;
            }

//            private static bool FParse
            public bool FAddByte(byte b)
            {
                m_rgbPacketRaw[m_ib++] = b;
                if (m_ib >= s_cbPacketFull)
                    {
                    return ParseRgb(this, m_rgbPacketRaw, s_cbPacketFull);
                    }
                return false;
            }


            
        }
        private UwtcSerialPort m_ser;

        public delegate void OnBytesReceived(byte[] rgb);
        public delegate void OnPacketReceived(UwtcPacket uwp);

        private OnBytesReceived m_obr;
        private OnPacketReceived m_opr;

        private UwtcPacket m_uwpInProgress;

        public Int64 ReadingIndex { get { return m_ser.ReadingIndex; } }
        void BackgroundRead()
        {
            LogSz(null, String.Format("Background Read top ({0})", System.Threading.Thread.CurrentThread.ManagedThreadId));

            byte[] rgb = new byte[1];

            m_uwpInProgress = new UwtcPacket();

            while (!threadStop.WaitOne(0))
                {
                byte b;
                b = (Byte)m_ser.ReadByte();

                // for now, we are going to send one byte at a time for POC
                rgb[0] = b;
                if (m_obr != null)
                    m_obr(rgb);

                if (m_uwpInProgress.FAddByte(b))
                    {
                    // this packet is done
                    if (m_opr != null)
                        m_opr(m_uwpInProgress);

                    m_uwpInProgress = new UwtcPacket();
                    }
                }
            m_ser.Close();
            m_ser = null;
        }

        private Thread m_t;

        public void Stop()
        {
            if (m_t != null)
                {
                threadStop.Set();
                if (!m_t.Join(10000))
                    m_t.Abort();
                m_t = null;
                }
        }

        protected ManualResetEvent threadStop = new ManualResetEvent(false);
        public void OpenPort(OnBytesReceived obr, OnPacketReceived opr, UwtcSerialPort ser)
        {
            m_ser = ser;
            m_obr = obr;
            m_opr = opr;

            Thread t = new Thread(BackgroundRead);
            m_t = t;
            t.Start();
        }
    }
}
