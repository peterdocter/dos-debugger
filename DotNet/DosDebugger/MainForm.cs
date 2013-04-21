﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Disassembler;
using X86Codec;
using Util.Forms;

namespace DosDebugger
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        MZFile mzFile;
        UInt16 baseSegment = 0x2920;

        private void MainForm_Load(object sender, EventArgs e)
        {
            //lvListing.SetWindowTheme("explorer");
            cbBookmarks.SelectedIndex = 1;
            string fileName = @"E:\Dev\Projects\DosDebugger\Reference\H.EXE";
            DoLoadFile(fileName);
        }

        private void DoLoadFile(string fileName)
        {
            mzFile = new MZFile(fileName);
            mzFile.Relocate(baseSegment);
            dasm = new Disassembler.Disassembler16(mzFile.Image, mzFile.BaseAddress);
            lvErrors.Items.Clear();
            lvListing.Items.Clear();
            lvProcedures.Items.Clear();
        }

        private void btnMzInfo_Click(object sender, EventArgs e)
        {
            ExecutableInfoForm f = new ExecutableInfoForm();
            f.MzFile = mzFile;
            f.Show(this);
        }

        Disassembler.Disassembler16 dasm;

        private void btnTest_Click(object sender, EventArgs e)
        {
            //TestDecode(mzFile.Image, mzFile.EntryPoint, mzFile.BaseAddress);
            //TestDecode(mzFile.Image, new FarPointer16(baseSegment, 0x17fc), mzFile.BaseAddress);
        }

        private void TestDecode(
            byte[] image,
            Pointer startAddress, 
            Pointer baseAddress)
        {
            DecoderContext options = new DecoderContext();
            options.AddressSize = CpuSize.Use16Bit;
            options.OperandSize = CpuSize.Use16Bit;

            X86Codec.Decoder decoder = new X86Codec.Decoder();

            Pointer ip = startAddress;
            for (int index = startAddress - baseAddress; index < image.Length; )
            {
                Instruction instruction = null;
                try
                {
                    instruction = decoder.Decode(image, index, ip, options);
                }
                catch (InvalidInstructionException ex)
                {
                    if (MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OKCancel)
                        == DialogResult.Cancel)
                    {
                        throw;
                    }
                    break;
                }
#if false
                // Output address.
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("0000:{0:X4}  ", index - startAddress);

                // Output binary code. */
                for (int i = 0; i < 8; i++)
                {
                    if (i < instruction.EncodedLength)
                        sb.AppendFormat("{0:x2} ", image[index + i]);
                    else
                        sb.Append("   ");
                }

                // Output the instruction.
                string s = instruction.ToString();
                if (s.StartsWith("*"))
                    throw new InvalidOperationException("Cannot format instruction.");
                sb.Append(s);

                System.Diagnostics.Debug.WriteLine(sb.ToString());
#else
                DisplayInstruction(instruction);
#endif
                index += instruction.EncodedLength;
                ip += instruction.EncodedLength;
            }
        }

        private static string FormatBinary(byte[] data, int startIndex, int count)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < count; i++)
            {
                if (i > 0)
                    sb.Append(' ');
                sb.AppendFormat("{0:x2}", data[startIndex + i]);
            }
            return sb.ToString();
        }

        private void btnAnalyze_Click(object sender, EventArgs e)
        {
            dasm.Analyze(mzFile.EntryPoint, true);
            X86Codec.Decoder decoder = new X86Codec.Decoder();

            // Display analyzed code.
            ByteAttributes[] attr = dasm.ByteAttributes;
            bool inCodeBlock = false;
            lvListing.Visible = false;
            for (int i = 0; i < attr.Length; )
            {
                if (attr[i].IsBoundary && attr[i].Type == ByteType.Code)
                {
                    DecoderContext context = new DecoderContext();
                    context.AddressSize = CpuSize.Use16Bit;
                    context.OperandSize = CpuSize.Use16Bit;

                    // TBD: the location parameter is incorrect.
                    Instruction insn = decoder.Decode(dasm.Image, i, mzFile.BaseAddress + i, context);
                    DisplayInstruction(insn);
                    i += insn.EncodedLength;
                    inCodeBlock = true;
                }
                else if (attr[i].IsBoundary && attr[i].Type == ByteType.Data)
                {
                    int j = i + 1;
                    while (attr[j].Type == ByteType.Data && !attr[j].IsBoundary)
                        j++;
                    DisplayData(mzFile.BaseAddress + i, j - i);
                    i = j;
                    inCodeBlock = true;
                }
                else
                {
                    if (inCodeBlock)
                    {
                        lvListing.Items.Add("");
                    }
                    inCodeBlock = false;
                    i++;
                }
            }
            lvListing.Visible = true;

            // Display subroutines.
            Pointer[] procEntries = dasm.Procedures;
            foreach (Pointer ptr in procEntries)
            {
                lvProcedures.Items.Add(ptr.ToString());
            }

            // Display analysis errors.
            foreach (Error error in dasm.Errors)
            {
                ListViewItem item = new ListViewItem();
                item.Text = error.Location.ToString();
                item.SubItems.Add(error.Message);
                lvErrors.Items.Add(item);
            }

            // Display status.
            txtStatus.Text = string.Format(
                "{0} procedures, {1} instructions, {2} errors",
                lvProcedures.Items.Count,
                lvListing.Items.Count,
                lvErrors.Items.Count);
        }

        private void DisplayInstruction(Instruction instruction)
        {
            Pointer start = instruction.Location;
            ListViewItem item = new ListViewItem();
            item.Text = string.Format(start.ToString());
            item.SubItems.Add(FormatBinary(mzFile.Image, start - mzFile.BaseAddress, instruction.EncodedLength));
            item.SubItems.Add(instruction.ToString());

            //item.UseItemStyleForSubItems = false;
            //item.SubItems[1].BackColor = Color.LightGray;
            lvListing.Items.Add(item);
        }

        private void DisplayData(Pointer start, int len)
        {
            ListViewItem item = new ListViewItem();
            item.Text = string.Format(start.ToString());
            item.SubItems.Add(FormatBinary(mzFile.Image, start - mzFile.BaseAddress, len));

            string data;
            int i = start - mzFile.BaseAddress;
            if (len == 1)
            {
                data = string.Format("db {0:x2}", mzFile.Image[i]);
            }
            else if (len == 2)
            {
                data = string.Format("dw {0:x4}", BitConverter.ToUInt16(mzFile.Image, i));
            }
            else
            {
                data = "** data **";
            }
            item.SubItems.Add(data);

            //item.UseItemStyleForSubItems = false;
            //item.SubItems[1].BackColor = Color.LightGray;
            lvListing.Items.Add(item);
        }

        private void btnGoTo_Click(object sender, EventArgs e)
        {
            // Find the address.
            Pointer target;
            string addr = cbBookmarks.Text;
            if (addr.Length < 9 || !Pointer.TryParse(addr.Substring(0, 9), out target))
            {
                MessageBox.Show(this, "The address '" + addr + "' is invalid.");
                return;
            }

            // Go to that location.
            if (!GoToLocation(target))
            {
                MessageBox.Show(this, "Cannot find that address.");
            }
        }

        private bool GoToLocation(Pointer target)
        {
            // Find the first entry that is greater than or equal to target.
            foreach (ListViewItem item in lvListing.Items)
            {
                Pointer current;
                if (!Pointer.TryParse(item.Text, out current))
                    continue;
                if (current.EffectiveAddress >= target.EffectiveAddress)
                {
                    //item.EnsureVisible();
                    lvListing.TopItem = item;
                    lvListing.Focus();
                    item.Selected = true;
                    return true;
                }
            }
            return false;
        }

        private void lvProcedures_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void lvProcedures_DoubleClick(object sender, EventArgs e)
        {
            if (lvProcedures.SelectedIndices.Count == 1)
            {
                GoToLocation(Pointer.Parse(lvProcedures.SelectedItems[0].Text));
            }
        }

        private void lvErrors_DoubleClick(object sender, EventArgs e)
        {
            if (lvErrors.SelectedIndices.Count == 1)
            {
                GoToLocation(Pointer.Parse(lvErrors.SelectedItems[0].Text));
            }
        }

        private void mnuFileExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void mnuFileOpen_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog(this) != DialogResult.OK)
                return;

            string fileName = openFileDialog1.FileName;
            DoLoadFile(fileName);
        }
    }
}
