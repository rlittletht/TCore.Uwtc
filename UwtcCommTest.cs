using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel.MsmqIntegration;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Windows.Forms;
using NUnit.Framework;

namespace TCore
{
    public partial class UwtcComm
    {
        public partial class UwtcPacket
        {
            [Test]
            [TestCase("00", "00", (UInt16) 0)]
            [TestCase("01", "00", (UInt16) 256)]
            [TestCase("00", "01", (UInt16) 1)]
            [TestCase("01", "01", (UInt16) 257)]
            [TestCase("00", "0A", (UInt16) 10)]
            [TestCase("0A", "00", (UInt16) 2560)]
            [TestCase("FF", "FF", (UInt16) 65535)]
            public void CombineStringBytesToNumberTest(string s1, string s2, UInt16 nResult)
            {
                Assert.AreEqual(nResult, CombineStringBytesToNumber(s1, s2));
            }

            private DateTime RobFromRedmond(DateTime dttmRedmond)
            {
                return dttmRedmond.AddHours(13).AddMinutes(30);
            }

            private DateTime RedmondFromRob(DateTime dttmRob)
            {
                return dttmRob.AddHours(-14).AddMinutes(30);
            }

            [Test]
            [TestCase("1/17/2015 1:16 AM", "1/16/2015 11:46 AM")]
            public void TestRob(string sRob, string sRedmond)
            {
                Assert.AreEqual(DateTime.Parse(sRob), RobFromRedmond(DateTime.Parse(sRedmond)));
                Assert.AreEqual(DateTime.Parse(sRedmond), RedmondFromRob(DateTime.Parse(sRob)));
            }

            [Test]
            [TestCase(0x00, 0x00, (UInt16) 0)]
            [TestCase(0x01, 0x00, (UInt16) 256)]
            [TestCase(0x00, 0x01, (UInt16) 1)]
            [TestCase(0x01, 0x01, (UInt16) 257)]
            [TestCase(0x00, 0x0A, (UInt16) 10)]
            [TestCase(0x0A, 0x00, (UInt16) 2560)]
            [TestCase(0xFF, 0xFF, (UInt16) 65535)]
            public void CombineBytesToNumberTest(byte bHigh, byte bLow, UInt16 nResult)
            {
                Assert.AreEqual(nResult, CombineBytesToNumber(bHigh, bLow));
            }

            [Test]
            [TestCase(new Byte[] {0x7e}, s_cbPacketFull, true)]
            [TestCase(new Byte[] {0x7f}, s_cbPacketFull, false)]
            [TestCase(new Byte[] {0x7e}, 0, false)]
            public void FParsePrologueTest(byte[] rgb, int cb, bool fResult)
            {
                Assert.AreEqual(fResult, FParsePrologue(rgb, cb));
            }

            [Test]
            [TestCase(new byte[] {0x7e, 0x00, 0x00}, s_cbPacketFull, 0, true)]
            [TestCase(new byte[] {0x7e, 0x00, 0x0a}, s_cbPacketFull, 10, true)]
            [TestCase(new byte[] {0x7e, 0x00, 0x01}, s_cbPacketFull, 1, true)]
            [TestCase(new byte[] {0x7e, 0x01, 0x00}, s_cbPacketFull, 256, true)]
            [TestCase(new byte[] {0x7e, 0x01, 0x01}, s_cbPacketFull, 257, true)]
            [TestCase(new byte[] {0x7e, 0x01, 0x01}, 3, 257, true)]
            [TestCase(new byte[] {0x7e, 0x01, 0x01}, 2, 0, false)]
            public void FParseLengthTest(byte[] rgb, int cb, Int16 wResult, bool fResult)
            {
                UInt16 w;
                Assert.AreEqual(fResult, FParseLength(rgb, cb, out w));
                Assert.AreEqual(wResult, w);
            }

            [Test]
            [TestCase(s_ibApiID, new byte[] {0x7e, 0x00, 0x00, 0x00}, s_cbPacketFull, 0x00, true)]
            [TestCase(s_ibApiID, new byte[] {0x7e, 0x00, 0x0a, 0x01}, s_cbPacketFull, 0x01, true)]
            [TestCase(s_ibApiID, new byte[] {0x7e, 0x01, 0x00, 0xff}, s_cbPacketFull, 0xff, true)]
            [TestCase(s_ibApiID, new byte[] {0x7e, 0x01, 0x00, 0xff}, s_ibApiID + 1, 0xff, true)]
            [TestCase(s_ibApiID, new byte[] {0x7e, 0x01, 0x01, 0xff}, s_ibApiID, 0x00, false)]
            [TestCase(s_ibApiID, new byte[] {0x7e, 0x01, 0x01, 0xff}, 0, 0x00, false)]
            [TestCase(s_ibSensorType, new byte[] {0x7e, 0x01, 0x01, 0xff, 0x00, 0x00, 0x00, 0x00, 0xC0}, s_cbPacketFull,
                0xC0, true)]
            [TestCase(s_ibSensorType, new byte[] {0x7e, 0x01, 0x01, 0xff, 0x00, 0x00, 0x00, 0x00, 0xC0},
                s_ibSensorType + 1, 0xC0, true)]
            [TestCase(s_ibSensorType, new byte[] {0x7e, 0x01, 0x01, 0xff, 0x00, 0x00, 0x00, 0x00, 0xC0}, s_ibSensorType,
                0x00, false)]
            public void FParseGenericByteTest(int ib, byte[] rgb, int cb, byte bResult, bool fResult)
            {
                byte b;
                Assert.AreEqual(fResult, FParseGenericByte(rgb, cb, ib, out b));
                Assert.AreEqual(bResult, b);
            }

            [Test]
            [TestCase(new byte[] {0x7e, 0x00, 0x00, 0x00, 0x00, 0x01}, s_cbPacketFull, (UInt16) 1, true)]
            [TestCase(new byte[] {0x7e, 0x00, 0x0a, 0x01, 0x01, 0x00}, s_cbPacketFull, (UInt16) 256, true)]
            [TestCase(new byte[] {0x7e, 0x01, 0x00, 0xff, 0xff, 0xff}, s_cbPacketFull, (UInt16) 65535, true)]
            [TestCase(new byte[] {0x7e, 0x01, 0x00, 0xff, 0x00, 0x01}, 6, (UInt16) 1, true)]
            [TestCase(new byte[] {0x7e, 0x01, 0x01, 0xff, 0x00, 0x01}, 5, (UInt16) 0, false)]
            [TestCase(new byte[] {0x7e, 0x01, 0x01, 0xff, 0x00, 0x01}, 0, (UInt16) 0, false)]
            public void FParseAddressTest(byte[] rgb, int cb, UInt16 wResult, bool fResult)
            {
                UInt16 w;
                Assert.AreEqual(fResult, FParseAddress(rgb, cb, out w));
                Assert.AreEqual(wResult, w);
            }

            [Test]
            [TestCase(new byte[] {0x7e, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x01}, s_cbPacketFull,
                (UInt16) 1, true)]
            [TestCase(new byte[] {0x7e, 0x00, 0x0a, 0x01, 0x01, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00}, s_cbPacketFull,
                (UInt16) 256, true)]
            [TestCase(new byte[] {0x7e, 0x01, 0x00, 0xff, 0xff, 0xff, 0x00, 0x00, 0x00, 0x01, 0x01}, s_cbPacketFull,
                (UInt16) 257, true)]
            [TestCase(new byte[] {0x7e, 0x01, 0x00, 0xff, 0xff, 0xff, 0x00, 0x00, 0x00, 0xFF, 0x01}, s_cbPacketFull,
                (UInt16) 0xFF01, true)]
            [TestCase(new byte[] {0x7e, 0x01, 0x00, 0xff, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x01},
                s_ibProcessDataLow + 1, (UInt16) 257, true)]
            [TestCase(new byte[] {0x7e, 0x01, 0x01, 0xff, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x01}, s_ibProcessDataLow,
                (UInt16) 0, false)]
            [TestCase(new byte[] {0x7e, 0x01, 0x01, 0xff, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x01}, 0, (UInt16) 0,
                false)]
            public void FParseProcessDataTest(byte[] rgb, int cb, UInt16 wResult, bool fResult)
            {
                UInt16 w;
                Assert.AreEqual(fResult, FParseProcessData(rgb, cb, out w));
                Assert.AreEqual(wResult, w);
            }

            [Test]
            [TestCase(
                new byte[]
                    {0x7E, 0x00, 0x0C, 0x81, 0x00, 0x01, 0x28, 0x00, 0x4B, 0x00, 0x43, 0x02, 0x7E, 0x0D, 0x30, 0x0A},
                s_cbPacketFull,
                (UInt16) s_cbPacketData, 0x81, (UInt16) 1, 0x28, 0x00, 0x4B, (UInt16) 0x0043, (UInt16) 0x027E,
                (UInt16) 0x0D30, 0x0A, true)]
            public void FParseRealWorldDataTest(
                byte[] rgb,
                int cb,
                UInt16 wLength,
                byte bApiID,
                UInt16 wAddress,
                byte bSignal,
                byte bReserved,
                byte bType,
                UInt16 wProcessData,
                UInt16 wAmbient,
                UInt16 wBattery,
                byte bChecksum,
                bool fResult)
            {
                UInt16 w;
                byte b;

                Assert.AreEqual(fResult, FParseLength(rgb, cb, out w));
                Assert.AreEqual(wLength, w);
                Assert.AreEqual(fResult, FParseApiID(rgb, cb, out b));
                Assert.AreEqual(bApiID, b);
                Assert.AreEqual(fResult, FParseAddress(rgb, cb, out w));
                Assert.AreEqual(wAddress, w);
                Assert.AreEqual(fResult, FParseSignalStrength(rgb, cb, out b));
                Assert.AreEqual(bSignal, b);
                Assert.AreEqual(fResult, FParseReserved(rgb, cb, out b));
                Assert.AreEqual(bReserved, b);
                Assert.AreEqual(fResult, FParseSensorType(rgb, cb, out b));
                Assert.AreEqual(bType, b);
                Assert.AreEqual(fResult, FParseProcessData(rgb, cb, out w));
                Assert.AreEqual(wProcessData, w);
                Assert.AreEqual(fResult, FParseAmbient(rgb, cb, out w));
                Assert.AreEqual(wAmbient, w);
                Assert.AreEqual(fResult, FParseBattery(rgb, cb, out w));
                Assert.AreEqual(wBattery, w);
                Assert.AreEqual(fResult, FParseChecksum(rgb, cb, out b));
                Assert.AreEqual(bChecksum, b);
            }
        }

    }

    public class SerialPortMock
    {
        private int m_msecLast;

        private static byte[] s_rgb1 = 
            //  0     1     2     3     4     5     6     7     8     9    10    11    12    13    14    15
            {0x7E, 0x00, 0x0C, 0x81, 0x00, 0x01, 0x28, 0x00, 0x4B, 0x00, 0x43, 0x02, 0x7E, 0x0D, 0x30, 0x0A,
             0x7E, 0x00, 0x0C, 0x81, 0x00, 0x02, 0x28, 0x00, 0x4B, 0x00, 0x40, 0x02, 0x70, 0x0D, 0x30, 0x0A,
             0x7E, 0x00, 0x0C, 0x81, 0x00, 0x03, 0x28, 0x00, 0x4B, 0x00, 0x49, 0x02, 0x72, 0x0D, 0x30, 0x0A};
        private static byte[] s_rgb2 = 
            {0x7E, 0x00, 0x0C, 0x81, 0x00, 0x01, 0x28, 0x00, 0x4B, 0x00, 0x44, 0x02, 0x7E, 0x0D, 0x30, 0x0A,
             0x7E, 0x00, 0x0C, 0x81, 0x00, 0x02, 0x28, 0x00, 0x4B, 0x00, 0x41, 0x02, 0x70, 0x0D, 0x30, 0x0A,
             0x7E, 0x00, 0x0C, 0x81, 0x00, 0x03, 0x28, 0x00, 0x4B, 0x00, 0x48, 0x02, 0x72, 0x0D, 0x30, 0x0A};
        private static byte[] s_rgb3 = 
            {0x7E, 0x00, 0x0C, 0x81, 0x00, 0x01, 0x28, 0x00, 0x4B, 0x00, 0x42, 0x02, 0x7E, 0x0D, 0x30, 0x0A,
             0x7E, 0x00, 0x0C, 0x81, 0x00, 0x02, 0x28, 0x00, 0x4B, 0x00, 0x43, 0x02, 0x70, 0x0D, 0x30, 0x0A,
             0x7E, 0x00, 0x0C, 0x81, 0x00, 0x03, 0x28, 0x00, 0x4B, 0x00, 0x49, 0x02, 0x72, 0x0D, 0x30, 0x0A};
        private static byte[] s_rgb4 = 
            {0x7E, 0x00, 0x0C, 0x81, 0x00, 0x01, 0x28, 0x00, 0x4B, 0x00, 0x43, 0x02, 0x7E, 0x0D, 0x30, 0x0A,
             0x7E, 0x00, 0x0C, 0x81, 0x00, 0x02, 0x28, 0x00, 0x4B, 0x00, 0x43, 0x02, 0x70, 0x0D, 0x30, 0x0A,
             0x7E, 0x00, 0x0C, 0x81, 0x00, 0x03, 0x28, 0x00, 0x4B, 0x00, 0x41, 0x02, 0x72, 0x0D, 0x30, 0x0A};
        private static byte[] s_rgb5 = 
            {0x7E, 0x00, 0x0C, 0x81, 0x00, 0x01, 0x28, 0x00, 0x4B, 0x00, 0x44, 0x02, 0x7E, 0x0D, 0x30, 0x0A,
             0x7E, 0x00, 0x0C, 0x81, 0x00, 0x02, 0x28, 0x00, 0x4B, 0x00, 0x43, 0x02, 0x70, 0x0D, 0x30, 0x0A,
             0x7E, 0x00, 0x0C, 0x81, 0x00, 0x03, 0x28, 0x00, 0x4B, 0x00, 0x46, 0x02, 0x72, 0x0D, 0x30, 0x0A};
        private static byte[] s_rgb6 = 
            {0x7E, 0x00, 0x0C, 0x81, 0x00, 0x01, 0x28, 0x00, 0x4B, 0x00, 0x4a, 0x02, 0x7E, 0x0D, 0x30, 0x0A,
             0x7E, 0x00, 0x0C, 0x81, 0x00, 0x02, 0x28, 0x00, 0x4B, 0x00, 0x3d, 0x02, 0x70, 0x0D, 0x30, 0x0A,
             0x7E, 0x00, 0x0C, 0x81, 0x00, 0x03, 0x28, 0x00, 0x4B, 0x00, 0x47, 0x02, 0x72, 0x0D, 0x30, 0x0A};
        private static byte[] s_rgb7 = 
            {0x7E, 0x00, 0x0C, 0x81, 0x00, 0x01, 0x28, 0x00, 0x4B, 0x00, 0x4f, 0x02, 0x7E, 0x0D, 0x30, 0x0A,
             0x7E, 0x00, 0x0C, 0x81, 0x00, 0x02, 0x28, 0x00, 0x4B, 0x00, 0x40, 0x02, 0x70, 0x0D, 0x30, 0x0A,
             0x7E, 0x00, 0x0C, 0x81, 0x00, 0x03, 0x28, 0x00, 0x4B, 0x00, 0x4c, 0x02, 0x72, 0x0D, 0x30, 0x0A};
        private static byte[] s_rgb8 = 
            {0x7E, 0x00, 0x0C, 0x81, 0x00, 0x01, 0x28, 0x00, 0x4B, 0x00, 0x51, 0x02, 0x7E, 0x0D, 0x30, 0x0A,
             0x7E, 0x00, 0x0C, 0x81, 0x00, 0x02, 0x28, 0x00, 0x4B, 0x00, 0x44, 0x02, 0x70, 0x0D, 0x30, 0x0A,
             0x7E, 0x00, 0x0C, 0x81, 0x00, 0x03, 0x28, 0x00, 0x4B, 0x00, 0x5c, 0x02, 0x72, 0x0D, 0x30, 0x0A};

        private static byte[][] s_rgrgb = { s_rgb1, s_rgb2, s_rgb3, s_rgb4, s_rgb5, s_rgb6, s_rgb7, s_rgb8 };

        private int m_irgb;
        private int m_ib;

        public void Close()
        {

        }

        public void Open()
        {
        }

        public class SimAtom
        {
            private RndFnParams[] m_rgrfp;
            // turn (0, 1) into (0, 1000)
            // (1, 4, 2) int ( 1/7, 4/7, 2/7)

            public int[] Normalize(int[] rgDist)
            {
                int i;
                int nMax = 0;

                for (i = 0; i < rgDist.Length; i++)
                    nMax += rgDist[i];

                int[] rgDistNew = new int[i];

                for (i = 0; i < rgDist.Length; i++)
                    rgDistNew[i] = (1000 * rgDist[i]) / nMax;

                return rgDistNew;
            }

            [Test]
            [TestCase(new int[] { 1 }, new int[]{ 1000 } )]
            [TestCase(new int[] { 1, 0 }, new int[]{ 1000, 0 } )]
            [TestCase(new int[] { 0, 1 }, new int[]{ 0, 1000 } )]
            [TestCase(new int[] { 1, 3}, new int[]{ 250, 750 } )]
            [TestCase(new int[] { 1, 2 }, new int[]{ 333, 666 } )]
            [TestCase(new int[] { 1, 0, 2 }, new int[]{ 333, 0, 666 } )]
            [TestCase(new int[] { 1, 2, 3 }, new int[]{ 166, 333, 500 } )]
            public void TestNormalize(int[] rgDist, int[] rgDistExpected)
            {
                int[] rgDistActual = Normalize(rgDist);

                Assert.AreEqual(rgDistExpected, rgDistActual);
            }

            struct RndFnParams
            {
                public double dRndMinInc;
                public double dRndMaxExc;
                public double dOutMinInc;
                public double dOutMaxExc;
            }

            RndFnParams[] BuildDistFormulas(int[] rgDist, double dMin, double dMax)
            {
                RndFnParams[] rgrfp = new RndFnParams[rgDist.Length];

                // we have an array of % distributions to be applied
                // linearly.  Assuming a random number returned [0,1),
                // figure out the right partitioning

                double dPartSize = ((double)dMax - (double)dMin) / rgDist.Length;
                double dRndMinCur = 0.0;
                double dOutMinCur = dMin;

                for (int i = 0; i < rgDist.Length; i++)
                    {
                    rgrfp[i].dOutMinInc = dOutMinCur;
                    rgrfp[i].dOutMaxExc = dOutMinCur += dPartSize;

                    // ok, now figure out the range of random numbers
                    // that will map to this...
                    rgrfp[i].dRndMinInc = dRndMinCur;
                    rgrfp[i].dRndMaxExc = dRndMinCur += (rgDist[i] / 1000.0);
                    }
                return rgrfp;
            }

            [Test]
            [TestCase(new int[] { 1000}, 0.0, 100.0, new double[] { 0.0, 1.0, 0.0, 100.0 } )]
            [TestCase(new int[] { 1000}, 0.0, 50.0, new double[] { 0.0, 1.0, 0.0, 50.0 } )]
            [TestCase(new int[] { 1000}, 50.0, 100.0, new double[] { 0.0, 1.0, 50.0, 100.0 } )]
            [TestCase(new int[] { 0, 1000}, 0.0, 100.0, new double[] { 0.0, 0.0, 0.0, 50.0,
                                                                   0.0, 1.0, 50.0, 100.0} )]
            [TestCase(new int[] { 500, 500}, 0.0, 100.0, new double[] { 0.0, 0.5, 0.0, 50.0,
                                                                    0.5, 1.0, 50.0, 100.0} )]
            [TestCase(new int[] { 250, 750}, 0.0, 100.0, new double[] { 0.0, 0.25, 0.0, 50.0,
                                                                    0.25, 1.0, 50.0, 100.0} )]
            [TestCase(new int[] { 200, 600, 200}, 0.0, 100.0, new double[] { 0.0, 0.2, 0.0, 33.33,
                                                                         0.2, 0.6, 33.33, 66.6,
                                                                         0.6, 1.0, 66.6, 100.0} )]
            [TestCase(new int[] { 100, 900}, -10.0, 100.0, new double[] { 0.0, 0.1, -10.0d, 45.0,
                                                                          0.1, 1.0, 45.0, 100.0} )]
            public void TestBuildDistFormulas(int[] rgDist, double dMin, double dMax, double[] rgdExpected)
            {
                RndFnParams[] rgrfp = BuildDistFormulas(rgDist, dMin, dMax);

                for (int i = 0; i < rgrfp.Length / 4 + 1; i++)
                    {
                    Assert.AreEqual(Math.Floor((1000.0d * rgdExpected[(i * 4) + 0] + 5) / 10), Math.Floor((100.0d * rgrfp[i].dRndMinInc)), "dRndMinInc mismatch");
                    Assert.AreEqual((int)((1000.0d * rgdExpected[(i * 4) + 1] + 5) / 10), (int)(100.0d * rgrfp[i].dRndMaxExc), "dRndMaxExc mismatch");
                    Assert.AreEqual(Math.Floor((1000.0d * rgdExpected[(i * 4) + 2] + 5) / 10), Math.Floor((100.0d * rgrfp[i].dOutMinInc)), "dOutMinInc mismatch");
                    Assert.AreEqual((int)((1000.0d * rgdExpected[(i * 4) + 3] + 5) / 10), (int)(100.0d * rgrfp[i].dOutMaxExc), "dOutMaxExc mismatch");
                    }
            }

            double AdjustNumForRfp(RndFnParams rfp, double dRnd)
            {
//                return (int)Math.Floor(((d - rfp.dRndMinInc) * ((rfp.dRndMaxExc - rfp.dRndMinInc) / (double)(dMax - dMin)) *
//                       100.0d * (rfp.dOutMaxExc - rfp.dOutMinInc) + rfp.dOutMinInc + 0.5d));

                return Math.Floor(((dRnd - rfp.dRndMinInc) / (rfp.dRndMaxExc - rfp.dRndMinInc) *
                                (rfp.dOutMaxExc - rfp.dOutMinInc)) +
                               rfp.dOutMinInc + 0.5d);
            }
            [Test]
            [TestCase(0.5d, 0.0d, 1.0d, 0.0d, 100.0d, 50.0)]
            [TestCase(0.5d, 0.5d, 1.0d, 50.0d, 100.0d, 50.0)]
            [TestCase(0.5d, 0.0d, 1.0d, 50.0d, 100.0d, 75.0)]
            [TestCase(0.7d, 0.0d, 1.0d, 50.0d, 100.0d, 85.0)]
            [TestCase(0.5d, 0.0d, 1.0d,  0.0d, 50.0d, 25.0)]
            [TestCase(0.4d, 0.0d, 0.5d,  0.0d, 50.0d, 40.0)]
            [TestCase(0.25d, 0.0d, 1.0d, 50.0d, 100.0d, 63.0)]
            [TestCase(0.0d, 0.0d, 1.0d, 50.0d, 100.0d, 50.0)]
            public void TestAdjustNumForRfp(double dRndIn, double dRndMinInc, double dRndMaxExc, double dOutMinInc, double dOutMaxExc, double dExpected)
            {
                RndFnParams rfp;

                rfp.dRndMinInc = dRndMinInc;
                rfp.dRndMaxExc = dRndMaxExc;
                rfp.dOutMinInc = dOutMinInc;
                rfp.dOutMaxExc = dOutMaxExc;

                Assert.AreEqual(dExpected, AdjustNumForRfp(rfp, dRndIn));
            }

            double ApplyDistToRnd(RndFnParams[] rgrfp, double dRnd)
            {
                // given a number from 0.0 to 1.0, return the adjust number given adjusted 
                // distribution
                int i = 0;

                while (i + 1 < rgrfp.Length && rgrfp[i + 1].dRndMinInc <= dRnd)
                    i++;

                return AdjustNumForRfp(rgrfp[i], dRnd);
            }

            [Test]
            [TestCase(0.5, 50.0, new double[] { 0.0, 1.0, 0.0, 100.0 }) ]
            [TestCase(0.5, 50.0, new double[] { 0.0, 0.5, 0.0, 50.0,
                                                0.5, 1.0, 50.0, 100.0 }) ]
            [TestCase(0.4, 40.0, new double[] { 0.0, 0.5, 0.0, 50.0,
                                                0.5, 1.0, 50.0, 100.0 }) ]
            [TestCase(0.99, 99.0, new double[] { 0.0, 0.5, 0.0, 50.0,
                                                 0.5, 1.0, 50.0, 100.0 }) ]
            [TestCase(0.50, 75.0, new double[] { 0.0, 0.0, 0.0, 50.0,
                                                 0.0, 1.0, 50.0, 100.0 }) ]
            [TestCase(0.00, 50.0, new double[] { 0.0, 0.0, 0.0, 50.0,
                                                 0.0, 1.0, 50.0, 100.0 }) ]
            [TestCase(0.50, 58.0, new double[] { 0.0, 0.2, 0.0, 33.33,
                                                 0.2, 0.6, 33.33, 66.6,
                                                 0.6, 1.0, 66.6, 100.0}) ]
            public void TestApplyDistToRnd(double dRndIn, double dExpected, double[] rgd)
            {
                RndFnParams[] rgrfp = new RndFnParams[rgd.Length / 4];

                for (int i = 0; i < rgd.Length / 4; i++)
                    {
                    rgrfp[i].dRndMinInc = rgd[i * 4];
                    rgrfp[i].dRndMaxExc = rgd[i * 4 + 1];
                    rgrfp[i].dOutMinInc = rgd[i * 4 + 2];
                    rgrfp[i].dOutMaxExc = rgd[i * 4 + 3];
                    }

                Assert.AreEqual(dExpected, ApplyDistToRnd(rgrfp, dRndIn));
            }

            private Random m_rnd;

            public double GenerateFromNum(double d)
            {
                return ApplyDistToRnd(m_rgrfp, d);
            }

            public double Generate()
            {
                double dVal = ApplyDistToRnd(m_rgrfp, m_rnd.NextDouble());
                return dVal;
            }

            public SimAtom()
            {
                m_rnd = new Random();
                m_rgrfp = BuildDistFormulas(new int[] {1000}, 0.0, 100.0);
            }

            public SimAtom(double dMin, double dMax, int[] rgDist)
            {
                m_rnd = new Random();

                // normalize rgDist into a real distribution
                int[] rgDistNorm = Normalize(rgDist);
                m_rgrfp = BuildDistFormulas(rgDistNorm, dMin, dMax);
            }

            [Test]
            public void TestSimAtomNull()
            {
                SimAtom sa = new SimAtom();

                Assert.AreEqual(50, sa.GenerateFromNum(0.5));
            }

            [Test]
            public void TestSimAtom()
            {
                SimAtom sa = new SimAtom(0.0, 100.0, new int[] {0, 1});

                Assert.AreEqual(75, sa.GenerateFromNum(.5));
                Assert.AreEqual(63, sa.GenerateFromNum(.25));
                Assert.AreEqual(50, sa.GenerateFromNum(0.0));
            }

            [Test]
            public void TestSimAtomNeg()
            {
                SimAtom sa = new SimAtom(-10.0, 30.0, new int[] {1, 3, 2, 1});

                Assert.AreEqual(-10, sa.GenerateFromNum(0.0));
                Assert.AreEqual(0, sa.GenerateFromNum(0.15));
                Assert.AreEqual(3.0, sa.GenerateFromNum(0.25));
                Assert.AreEqual(7.0, sa.GenerateFromNum(0.45));
            }
        }

        public class SimReading
        {
            private double m_dCur;
            private struct Rule
            {
                public RuleKind rk;
                public  double dVal;
                public SimAtom sa;
            };

            private int m_irCur;
            private List<Rule> m_plr;
            public enum RuleKind
            {
                ValueGreater,
                ValueLess,
                Always
            }

            public void AddRule(RuleKind rk, double dVal, double dMin, double dMax, int[] rgDist)
            {
                Rule r;

                r.rk = rk;
                r.dVal = dVal;
                r.sa = new SimAtom(dMin, dMax, rgDist);

                m_plr.Add(r);
            }

            public SimReading(double dMinStart, double dMaxStart, int[] rgnDistGrowth, double dMinGrow, double dMaxGrow)
            {
                m_plr = new List<Rule>();
                Rule r;

                r.rk = RuleKind.Always;
                r.dVal = 0;
                r.sa = new SimAtom(dMinGrow, dMaxGrow, rgnDistGrowth);
                m_plr.Add(r);
                m_dCur = new Random().NextDouble() * (dMaxStart - dMinStart) + dMinStart;
            }


            public double Reading { get { return m_dCur;  } }
            public double BumpReading()
            {
                m_dCur += (int)m_plr[m_irCur].sa.Generate();

                if (m_irCur + 1 < m_plr.Count)
                    {
                    if (m_plr[m_irCur + 1].rk == RuleKind.Always)
                        m_irCur++;
                    else if (m_plr[m_irCur + 1].rk == RuleKind.ValueGreater && m_dCur > m_plr[m_irCur + 1].dVal)
                        m_irCur++;
                    else if (m_plr[m_irCur + 1].rk == RuleKind.ValueLess && m_dCur < m_plr[m_irCur + 1].dVal)
                        m_irCur++;
                    }

                return Reading;
            }
        }

        public class Sim
        {
            private SimAtom m_saDelay;

            private SimReading m_srAmbient;
            private SimReading m_srTherm1;
            private SimReading m_srTherm2;
            private SimReading m_srTherm3;
            private Int64 m_cSteps;

            public Sim(int msecMin, int msecMax, int[] rgnDistDelay)
            {
                m_saDelay = new SimAtom(msecMin, msecMax, rgnDistDelay);
            }

            public void SetupAmbient(double dMinStart, double dMaxStart, int[] rgnDistGrowth, double dMinGrow, double dMaxGrow)
            {
                m_srAmbient = new SimReading(dMinStart, dMaxStart, rgnDistGrowth, dMinGrow, dMaxGrow);
            }
            public void AddAmbientRule(SimReading.RuleKind rk, double dVal, double dMinGrow, double dMaxGrow, int[] rgnDistGrowth)
            {
                m_srAmbient.AddRule(rk, dVal, dMinGrow, dMaxGrow, rgnDistGrowth);
            }

            public void SetupTherm1(double dMinStart, double dMaxStart, int[] rgnDistGrowth, double dMinGrow, double dMaxGrow)
            {
                m_srTherm1 = new SimReading(dMinStart, dMaxStart, rgnDistGrowth, dMinGrow, dMaxGrow);
            }

            public void AddTherm1Rule(SimReading.RuleKind rk, double dVal, double dMinGrow, double dMaxGrow, int[] rgnDistGrowth)
            {
                m_srTherm1.AddRule(rk, dVal, dMinGrow, dMaxGrow, rgnDistGrowth);
            }

            public void SetupTherm2(double dMinStart, double dMaxStart, int[] rgnDistGrowth, double dMinGrow, double dMaxGrow)
            {
                m_srTherm2 = new SimReading(dMinStart, dMaxStart, rgnDistGrowth, dMinGrow, dMaxGrow);
            }

            public void AddTherm2Rule(SimReading.RuleKind rk, double dVal, double dMinGrow, double dMaxGrow, int[] rgnDistGrowth)
            {
                m_srTherm2.AddRule(rk, dVal, dMinGrow, dMaxGrow, rgnDistGrowth);
            }

            public void SetupTherm3(double dMinStart, double dMaxStart, int[] rgnDistGrowth, double dMinGrow, double dMaxGrow)
            {
                m_srTherm3 = new SimReading(dMinStart, dMaxStart, rgnDistGrowth, dMinGrow, dMaxGrow);
            }

            public void AddTherm3Rule(SimReading.RuleKind rk, double dVal, double dMinGrow, double dMaxGrow, int[] rgnDistGrowth)
            {
                m_srTherm3.AddRule(rk, dVal, dMinGrow, dMaxGrow, rgnDistGrowth);
            }

            public void Step()
            {
                if (m_srAmbient != null)
                    m_srAmbient.BumpReading();
                if (m_srTherm1 != null)
                    m_srTherm1.BumpReading();
                if (m_srTherm2 != null)
                    m_srTherm2.BumpReading();
                if (m_srTherm3 != null)
                    m_srTherm3.BumpReading();
                m_cSteps++;
            }

            public Int64 StepIndex { get { return m_cSteps; } }
            public double Ambient { get { return m_srAmbient == null ? -33 : m_srAmbient.Reading; }}
            public double Therm1 { get { return m_srTherm1 == null ? -33 : m_srTherm1.Reading; }}
            public double Therm2 { get { return m_srTherm2 == null ? -33 : m_srTherm2.Reading; }}
            public double Therm3 { get { return m_srTherm3 == null ? -33 : m_srTherm3.Reading; }}

            public int Delay { get { return (int)m_saDelay.Generate();  } }
        }

        public Int64 StepIndex { get { return m_sim.StepIndex; } }

        // private int m_msecDelay = 5000;
        private Sim m_sim;
        private int m_nAddrLast; 

        bool IsByteIndex(int ib, int ibMatch)
        {
            if ((ib % 16) == ibMatch)
                return true;

            return false;
        }

        [Test]
        [TestCase(5, 5, true)]
        [TestCase(21, 5, true)]
        [TestCase(37, 5, true)]
        [TestCase(26, 10, true)]
        [TestCase(0, 0, true)]
        [TestCase(16, 0, true)]
        [TestCase(1, 0, false)]
        public void TestIsByteIndex(int ib, int ibMatch, bool fExpected)
        {
            Assert.AreEqual(fExpected, IsByteIndex(ib, ibMatch));
        }

        [Test]
        public void TestFullMock()
        {
            //                                                    -1         0       1       2
            SimReading sr1 = new SimReading(50.0, 60.0, new int[] {1, 2, 15, 170, 16, 2, 1, 1, 1}, -1.0, 2.0);
//            SimReading sr1 = new SimReading(50.0, 60.0, new int[] {1}, -1.0, 1.0);

            sr1.AddRule(SimReading.RuleKind.ValueGreater, 160.0, -1.0, 1.0, new int[] {1, 1, 5, 850, 2, 1, 1});
            sr1.AddRule(SimReading.RuleKind.ValueGreater, 170.0, -1.0, 2.0, new int[] {1, 2, 36, 870, 16, 2, 1, 1, 1});
            
            //                                                    -3 -2 -1  0  1  2  3  4   
            SimReading sr2 = new SimReading(50.0, 60.0, new int[] {1, 4, 35, 58, 10, 5, 1}, -3.0, 4.0);

            sr2.AddRule(SimReading.RuleKind.ValueGreater, 160.0, -1.0, 1.0, new int[] {1, 1, 5, 850, 2, 1, 1});
            sr2.AddRule(SimReading.RuleKind.ValueGreater, 170.0, -3.0, 4.0, new int[] {1, 6, 465, 458, 10, 5, 1});
            
            //                                                    -2     0     2      4
            SimReading sr3 = new SimReading(50.0, 60.0, new int[] {1, 8, 44, 13, 5, 2, 1}, -2.0, 4.0);
            sr3.AddRule(SimReading.RuleKind.ValueGreater, 160.0, -1.0, 1.0, new int[] {1, 1, 5, 850, 2, 1, 1});
            sr3.AddRule(SimReading.RuleKind.ValueGreater, 170.0, -2.0, 2.0, new int[] {1, 8, 44, 43, 45, 9, 1});

            //                                                    -3  -2  -1   0   1   2   3   4   5
            SimReading sr4 = new SimReading(50.0, 60.0, new int[] {1, 3, 19, 73, 15, 5, 1, 1, 1, 1, 1}, -2.0, 5.0);
            sr4.AddRule(SimReading.RuleKind.ValueGreater, 240.0, -1.0, 1.0, new int[] {1, 1, 5, 850, 1, 1, 1});

            // do 5000 steps
            StreamWriter sw = new StreamWriter("c:\\temp\\test.txt", false, System.Text.Encoding.Default);

            for (int i = 0; i < 10000; i++)
                {
                sr1.BumpReading();
                sr2.BumpReading();
                sr3.BumpReading();
                sr4.BumpReading();
                sw.WriteLine(String.Format("{0},{1},{2},{3}", sr1.Reading, sr2.Reading, sr3.Reading, sr4.Reading));
                }
            sw.Close();
        }
        public SerialPortMock() { }

        public Byte ReadByte()
        {
            int msec = System.Environment.TickCount;
            int msecDelay = m_sim.Delay;

            // only sleep at the beginning of the buffer
            if (m_msecLast < msec && m_msecLast + msecDelay > msec && m_ib == 0)
                {
                    Thread.Sleep((m_msecLast + msecDelay) - msec);
                }

            if (m_ib == 0)
                m_sim.Step();

            Byte b = s_rgrgb[m_irgb][m_ib];
            if (IsByteIndex(m_ib, UwtcComm.UwtcPacket.s_ibAddressLow))
                {
                m_nAddrLast = b;
                }
            else if (IsByteIndex(m_ib, UwtcComm.UwtcPacket.s_ibProcessDataLow) || IsByteIndex(m_ib, UwtcComm.UwtcPacket.s_ibProcessDataHigh))
                {
                double d = -33.0;
                if (m_nAddrLast == 1)
                    d = m_sim.Therm1;
                else if (m_nAddrLast == 2)
                    d = m_sim.Therm2;
                else if (m_nAddrLast == 3)
                    d = m_sim.Therm3;

                if (d > 0.0)
                    {
                    int n = (int) d;

                    if (IsByteIndex(m_ib, UwtcComm.UwtcPacket.s_ibProcessDataHigh))
                        n = n >> 8;
                    else
                        n = n & 0x00ff;

                    b = (byte)n;
                    }
            }
            else if (IsByteIndex(m_ib, UwtcComm.UwtcPacket.s_ibAmbientLow) || IsByteIndex(m_ib, UwtcComm.UwtcPacket.s_ibAmbientHigh))
                {
                double d = m_sim.Ambient;

                if (d > 0.0)
                    {
                    int n = (int) (d * 100.0);

                    if (IsByteIndex(m_ib, UwtcComm.UwtcPacket.s_ibAmbientHigh))
                        n = n >> 8;
                    else
                        n = n & 0x00ff;

                    b = (byte) n;
                    }
                }
            m_ib++;

            if (m_ib >= s_rgrgb[m_irgb].Length)
                {
                m_ib = 0;
                m_irgb++;
                if (m_irgb >= s_rgrgb.Length)
                    m_irgb = 0;
                }
            m_msecLast = msec;
            return b;
        }

        public SerialPortMock(string sPort, int nBps, Parity par, int nBits, StopBits sb)
        {
            m_sim = new Sim(3500, 25500, new int[] { 6, 10, 18, 16, 14, 12, 8, 6, 6, 4, 3, 3, 2, 2, 1, 1, 1, 1 } );
//            m_sim = new Sim(23, 123, new int[] { 6, 10, 18, 16, 14, 12, 8, 6, 6, 4, 3, 3, 2, 2, 1, 1, 1, 1 } );

            //                                       -1         0       1       2
            m_sim.SetupAmbient(50.0, 60.0, new int[] {1, 2, 15, 70, 17, 2, 1, 1, 1}, -1.0, 2.0);
            m_sim.AddAmbientRule(SimReading.RuleKind.ValueGreater, 240.0, -1.0, 1.0, new int[] {1, 1, 5, 850, 1, 1, 1});
            //                                      -3 -2 -1  0  1  2  3  4   
            m_sim.SetupTherm1(50.0, 60.0, new int[] {1, 4, 35, 58, 10, 5, 1}, -3.0, 4.0);
            m_sim.AddTherm1Rule(SimReading.RuleKind.ValueGreater, 160.0, -1.0, 1.0, new int[] {1, 1, 5, 850, 2, 1, 1});
            m_sim.AddTherm1Rule(SimReading.RuleKind.ValueGreater, 170.0, -1.0, 2.0, new int[] {1, 2, 36, 870, 16, 2, 1, 1, 1});
            //                                       -2     0     2      4
            m_sim.SetupTherm2(50.0, 60.0, new int[] {1, 8, 44, 13, 5, 2, 1}, -2.0, 4.0);
            m_sim.AddTherm2Rule(SimReading.RuleKind.ValueGreater, 160.0, -1.0, 1.0, new int[] {1, 1, 5, 850, 2, 1, 1});
            m_sim.AddTherm2Rule(SimReading.RuleKind.ValueGreater, 170.0, -3.0, 4.0, new int[] {1, 6, 465, 458, 10, 5, 1});
            //                                      -3  -2  -1   0   1   2   3   4   5
            m_sim.SetupTherm3(50.0, 60.0, new int[] {1, 3, 19, 73, 15, 5, 1, 1, 1, 1, 1}, -2.0, 5.0);
            m_sim.AddTherm3Rule(SimReading.RuleKind.ValueGreater, 160.0, -1.0, 1.0, new int[] {1, 1, 5, 850, 2, 1, 1});
            m_sim.AddTherm3Rule(SimReading.RuleKind.ValueGreater, 170.0, -2.0, 2.0, new int[] {1, 8, 44, 43, 45, 9, 1});
        }



    }
}
