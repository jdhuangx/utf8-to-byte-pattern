﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Threading;

namespace wf_test
{
    public partial class Form1 : Form
    {
        //int fontSize = 14;
        int fontSize = 14;

        Font drawFont;
        SolidBrush drawBrush;
        StringFormat drawFormat;

        Bitmap tmpBitmap;
        Graphics tmpGraphicForMeas;
        SizeF stringSize = new SizeF();

        int fontHeight;
        int fontWeight;

        //https://blog.longwin.com.tw/2013/12/unicode-utf8-char-range-table-2013/
        int[] requireRangeMin = { 0x0020, 0x002E80 };
        int[] requireRangeMax = { 0x007f, 0x00FFFF };
        int[] byteCountArray = { 1, 3 };

        /*
        int[] requireRangeMin = { 0x0020 };
        int[] requireRangeMax = { 0x007f };
        int[] byteCountArray = { 1};
        */

        public Form1()
        {
            InitializeComponent();

            initParam();

        }

        private void initParam() {
            drawFont = new Font("Arial", fontSize);
            drawBrush = new SolidBrush(System.Drawing.Color.White);

            tmpBitmap = new Bitmap(10, 10);
            tmpGraphicForMeas = Graphics.FromImage(tmpBitmap);
            stringSize = tmpGraphicForMeas.MeasureString("測試", drawFont);
            fontHeight = (int)Math.Ceiling(stringSize.Height);
        }

        private void drawOneChar(char c, Bitmap bmp) {
            Graphics g = Graphics.FromImage(bmp);

            g.Clear(Color.Black);
            g.DrawString(c.ToString(), drawFont, drawBrush, 0, 0);
            g.Flush();
        }

        private int saveCharInfo(FileStream info, char c, Bitmap bmp) {
            drawOneChar(c,bmp);

            stringSize = tmpGraphicForMeas.MeasureString(c.ToString(), drawFont);
            fontHeight = (int)Math.Ceiling(stringSize.Height);
            fontWeight = (int)Math.Ceiling(stringSize.Width);

            //測試顯示font size=14的時候，實際的文字高度為24，用3byte表示一列pixel
            //後來我改為一個pixel記錄一個byte做灰階
            int byteCount = 0;
            for (int x = 0; x < fontWeight; x++) {
                //check colume
                
                bool isBlankLine = false;
                for (int y = 0; y < fontHeight; y++)
                {
                    if ((byte)bmp.GetPixel(x, y).R>0) {
                        isBlankLine = true;
                        break;
                    }
                }
                if (isBlankLine == false) {
                    continue;
                }
                for (int y = 0; y < fontHeight; y++)
                {
                    int test = (bmp.GetPixel(x, y).R & 0x00ff);
                    info.WriteByte((byte)(bmp.GetPixel(x, y).R & 0x00ff));
                    byteCount++;
                }
            }
            return byteCount;
        }

        private void saveMap(FileStream mapSw, int i, int idx,int byteCount) {
            //char utf8 address
            int byte1 = ((i) >> 24) & 0x00ff;
            int byte2 = ((i) >> 16) & 0x00ff;
            int byte3 = ((i) >> 8) & 0x00ff;
            int byte4 = (i & 0x00ff);
            mapSw.WriteByte((byte)byte1);
            mapSw.WriteByte((byte)byte2);
            mapSw.WriteByte((byte)byte3);
            mapSw.WriteByte((byte)byte4);
            //address in font info file
            byte1 = ((idx) >> 24) & 0x00ff;
            byte2 = ((idx) >> 16) & 0x00ff;
            byte3 = ((idx) >> 8) & 0x00ff;
            byte4 = (idx & 0x00ff);
            mapSw.WriteByte((byte)byte1);
            mapSw.WriteByte((byte)byte2);
            mapSw.WriteByte((byte)byte3);
            mapSw.WriteByte((byte)byte4);
            //size
            byte1 = ((byteCount) >> 8) & 0x00ff;
            byte2 = (byteCount & 0x00ff);
            mapSw.WriteByte((byte)byte1);
            mapSw.WriteByte((byte)byte2);
        }
        /*
        private void generateStringInfo() {
            FileStream infoSw = new FileStream("fontInfo.bin", FileMode.Create);
            FileStream mapSw = new FileStream("fontMap.txt", FileMode.Create);
            Bitmap bmp = new Bitmap(fontHeight * 2, fontHeight*2);

            //create ascii info
            int idx = 0;
            int byteCount = 0;
            for (int i = 32; i < 127; i++) {
                char c=Convert.ToChar(i);
                byteCount = saveCharInfo(idx,infoSw,c, bmp);
                saveMap(mapSw,i,idx, byteCount);
                idx += byteCount;
            }

            infoSw.Flush();
            infoSw.Close();
            mapSw.Flush();
            mapSw.Close();

            testString();
        }*/

        private void getUtf8BytesFromIdx(int bytes, int idx, byte[] data) {
            int tmp = 0;

            switch (bytes) { 
                case 1:
                    data[0] = (byte)(idx & 0x00ff);
                    break;
                case 2:
                    tmp = (0xC0) + ((idx >> 6) & 0x001F);
                    data[0] =(byte)(tmp & 0x00ff);
                    tmp = (0x80) + ((idx) & 0x003F);
                    data[1] = (byte)(tmp & 0x00ff);
                    break;
                case 3:
                    tmp = (0xE0) + ((idx >> 12) & 0x000F);
                    data[0] =(byte)(tmp & 0x00ff);
                    tmp = (0x80) + ((idx>>6) & 0x003F);
                    data[1] = (byte)(tmp & 0x00ff);
                    tmp = (0x80) + ((idx) & 0x003F);
                    data[2] = (byte)(tmp & 0x00ff);
                    break;
                case 4:
                    tmp = (0xF0) + ((idx >> 18) & 0x0007);
                    data[0] =(byte)(tmp & 0x00ff);
                    tmp = (0x80) + ((idx>>12) & 0x003F);
                    data[1] = (byte)(tmp & 0x00ff);
                    tmp = (0x80) + ((idx) & 0x003F);
                    data[2] = (byte)(tmp & 0x00ff);
                    break;
            }
        }

        private void testUtf8() {
            generateUtf8StringInfo(byteCountArray, requireRangeMin, requireRangeMax);
        }

        delegate void GuiHelper(PictureBox pb,Bitmap bmp,int idx);
        private void updateGui(PictureBox pb, Bitmap bmp, int idx)
        {
            if (pb.InvokeRequired)
            {
                GuiHelper gh = new GuiHelper(updateGui);
                pb.Invoke(gh,pb,bmp, idx);
            }
            else {
                label1.Text = "idx: " + idx;
                pb.Image = bmp;
            }
        }

        private void generateUtf8StringInfo(int[] bytesCount,int[] minAddr,int[] maxAddr)
        {
            FileStream infoSw = new FileStream("fontInfo.bin", FileMode.Create);
            FileStream mapSw = new FileStream("fontMap.txt", FileMode.Create);

            //save 100 byte for meta data
            mapSw.Seek(100,SeekOrigin.Begin);
            int[] beginAddressOfMapSegment = new int[bytesCount.Length];

            Encoding utf8 = Encoding.UTF8;

            int fileIdx = 0;
            int mapIdx = 0;
            for(int a = 0; a < minAddr.Length; a++) {
                byte[] ba = new byte[bytesCount[a]];
                beginAddressOfMapSegment[a] = (int)mapSw.Position;

                for (int i = minAddr[a]; i <= maxAddr[a]; i++)
                {
                    getUtf8BytesFromIdx(bytesCount[a], i, ba);
                    char[] chars = utf8.GetChars(ba);

                    Bitmap bmp = new Bitmap(fontHeight * 2, fontHeight * 2);
                    drawOneChar(chars[0], bmp);

                    int byteCount = saveCharInfo(infoSw, chars[0], bmp);
                    saveMap(mapSw, i, fileIdx, byteCount);
                    fileIdx += byteCount;


                    updateGui(pictureBox, bmp, i);
                    Thread.Sleep(5);
                }
            }


            //save meta data
            //[font segment begin address][address of each segment in map file]
            mapSw.Seek(0, SeekOrigin.Begin);
            mapSw.WriteByte((byte)(bytesCount.Length));
            for (int i = 0; i < bytesCount.Length; i++)
            {
                int beginAddr = minAddr[i];
                int byte1 = ((beginAddr) >> 24) & 0x00ff;
                int byte2 = ((beginAddr) >> 16) & 0x00ff;
                int byte3 = ((beginAddr) >> 8) & 0x00ff;
                int byte4 = (beginAddr & 0x00ff);
                mapSw.WriteByte((byte)byte1);
                mapSw.WriteByte((byte)byte2);
                mapSw.WriteByte((byte)byte3);
                mapSw.WriteByte((byte)byte4);

                int mapAddress = beginAddressOfMapSegment[i];
                byte1 = ((mapAddress) >> 24) & 0x00ff;
                byte2 = ((mapAddress) >> 16) & 0x00ff;
                byte3 = ((mapAddress) >> 8) & 0x00ff;
                byte4 = (mapAddress & 0x00ff);
                mapSw.WriteByte((byte)byte1);
                mapSw.WriteByte((byte)byte2);
                mapSw.WriteByte((byte)byte3);
                mapSw.WriteByte((byte)byte4);
            }

            infoSw.Flush();
            infoSw.Close();
            mapSw.Flush();
            mapSw.Close();
        }

        private void testString() {
            FileStream infoSw = new FileStream("fontInfo.bin", FileMode.Open);
            FileStream mapSw = new FileStream("fontMap.txt", FileMode.Open);

            String test = "R";
            int utf8Idx= Convert.ToInt32(test[0]);

            //read meta data
            int segmentCount = mapSw.ReadByte();
            int[] fontbeginAddress = new int[segmentCount];
            int[] mapBeginAddress = new int[segmentCount];
            for (int i = 0; i < segmentCount; i++) {
                int aaa = 0;
                aaa += mapSw.ReadByte(); aaa <<= 8;
                aaa += mapSw.ReadByte(); aaa <<= 8;
                aaa += mapSw.ReadByte(); aaa <<= 8;
                aaa += mapSw.ReadByte();
                fontbeginAddress[i] = aaa;

                aaa = 0;
                aaa += mapSw.ReadByte(); aaa <<= 8;
                aaa += mapSw.ReadByte(); aaa <<= 8;
                aaa += mapSw.ReadByte(); aaa <<= 8;
                aaa += mapSw.ReadByte();
                mapBeginAddress[i] = aaa;
            }

            int selectSegment = (segmentCount - 1);
            for (int i = 0; i < (segmentCount-1); i++) {
                if(utf8Idx>= fontbeginAddress[i] && utf8Idx < fontbeginAddress[i + 1])
                {
                    selectSegment = i;
                    break;
                }
            }
            utf8Idx -= fontbeginAddress[selectSegment];

            mapSw.Seek((utf8Idx * 10)+4+ mapBeginAddress[selectSegment], SeekOrigin.Begin);
            int idx = 0;
            idx += mapSw.ReadByte(); idx <<= 8;
            idx += mapSw.ReadByte(); idx <<= 8;
            idx += mapSw.ReadByte(); idx <<= 8;
            idx += mapSw.ReadByte();
            int byteCount = 0;
            byteCount += mapSw.ReadByte(); byteCount <<= 8;
            byteCount += mapSw.ReadByte();

            infoSw.Seek(idx, SeekOrigin.Begin);
            byte[] data = new byte[byteCount];
            infoSw.Read(data,0, byteCount);

            drawInfoToBitmap(data,byteCount);

            infoSw.Flush();
            infoSw.Close();
            mapSw.Flush();
            mapSw.Close();
        }

        private void drawInfoToBitmap(byte[] info,int byteCount) {
            int width = byteCount / fontHeight;

            Bitmap bmp = new Bitmap(width, fontHeight);

            int idx = 0;
            for (int x = 0; x < width; x++) {

                for (int y = 0; y < fontHeight; y++) {
                    byte b = (byte)info[idx++];

                    if (b == 0)
                    {
                        bmp.SetPixel(x, y, Color.Black);
                    }
                    else {
                        Color color = Color.FromArgb(255, b, b, b);
                        bmp.SetPixel(x, y, color);
                    }

                }
            }
            pictureBox.Image = bmp;
            pictureBox1.Image = bmp;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            initParam();

            String str = textBox1.Text;

            stringSize = tmpGraphicForMeas.MeasureString(str[0].ToString(), drawFont);
            fontHeight = (int)Math.Ceiling(stringSize.Height);
            fontWeight = (int)Math.Ceiling(stringSize.Width);

            Bitmap bmp = new Bitmap(fontWeight, fontHeight);
            drawOneChar(str[0],bmp);

            pictureBox.Image = bmp;
            testString();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //generateStringInfo();

            //generateUtf8StringInfo(3);
            Thread t = new Thread(testUtf8);
            t.Start();
        }
    }
}
