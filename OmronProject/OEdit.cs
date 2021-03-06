﻿using System.Diagnostics.CodeAnalysis;

namespace OmronProject
{
    using System;
    using System.Drawing;
    using System.Runtime.InteropServices;
    using System.Windows.Forms;

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "SpecifyACultureInStringConversionExplicitly")]
    public partial class OEdit : TextBox
    {
        public enum TActivity
        {
            ActInAct,
            PasInAct,
            PasInPas
        }

        public enum TState
        {
            Normal,
            Proc,
            WaitSend,
            Error,
            NoData
        }

        public enum ContentType
        {
            NumInt,
            NumFloat,
            Automan,
            OnOff,
            Mode
        }

        public enum TWindow
        {
            Dno,
            Bok,
            Pit,
            Algorithm,
            GrowParam
        }

        private enum Operation { Increment, Decrement }

        public int MinError { get; set; }
        public int MaxError { get; set; }
        public float Step { get; set; }
        public int NumsAfterDot { get; set; }
        public ContentType TypeContent { get; set; }
        public TActivity Activity { get; set; }
        public TState State { get; set; }
        public TWindow WindowAttachment { get; set; }
        public OEdit FieldUp { get; set; }
        public OEdit FieldDown { get; set; }
        public OPanel PanelHeater { get; set; }

        public OEdit()
        {
            InitializeComponent();

            GotFocus += FieldGotFocus;
            KeyPress += TestKeyPress;
        }

        private void TestKeyPress(object sender, KeyPressEventArgs e)
        {
            throw new NotImplementedException();
        }

        [DllImport("user32.dll")]
        private static extern bool HideCaret(IntPtr hWnd);

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x201 || m.Msg == 0x204 || m.Msg == 0x203) //0x201 - Left Button 0x204 - Right button
            {
                return;
            }
            base.WndProc(ref m);
        }

        private void FieldGotFocus(object sender, EventArgs args)
        {
            //   HideCaret(Handle);
            if (PanelHeater.IsActive)
                BackColor = Color.Yellow;
            SelectionStart = TextLength;
        }

        private void OmronEditKeyPress(object sender, KeyPressEventArgs e)
        {
            bool isDotChar = e.KeyChar == ',';

            if (e.KeyChar == '.')
            {
                e.Handled = true;
                if (Text.IndexOf(',') < 0 && SelectionStart != 0 && TypeContent == ContentType.NumFloat)
                {
                    Text += @",";
                    SelectionStart = Text.Length;
                }
            }

            if ((Text.IndexOf(',') >= 0 || SelectionStart == 0) && isDotChar || e.KeyChar == '-')
            {
                e.Handled = true;
            }

            if (Text.IndexOf('0') == 0 && (e.KeyChar == '0' || !isDotChar) && SelectionStart == 1 || (SelectionStart == 0 && Text == @"0"))
            {
                Text = "";
            }
            BackColor = Color.Red;
        }

        private void CheckInput(KeyEventArgs e)
        {
            var MaxTextLength = 6;
            var MaxFract = 2;

            bool MissRange0_9 = e.KeyCode < Keys.D0 || e.KeyCode > Keys.D9;
            bool MissRange0_9Numpad = e.KeyCode < Keys.NumPad0 || e.KeyCode > Keys.NumPad9;
            bool DotPressed = e.KeyCode == Keys.OemPeriod || e.KeyCode == Keys.Decimal || e.KeyCode == Keys.Oemcomma;
            bool MinusPressed = e.KeyCode == Keys.OemMinus || e.KeyCode == Keys.Subtract;
            bool ZeroPressed = e.KeyCode == Keys.D0 || e.KeyCode == Keys.NumPad0;
            bool BackPressed = e.KeyCode == Keys.Back;
            bool DotInField = Text.IndexOf(',') >= 0;
            //     bool FractIsMax = Text.Substring(Text.IndexOf('.')).Length > MaxFract;

            if (MissRange0_9 && MissRange0_9Numpad && !DotPressed && !MinusPressed && !BackPressed)
            {
                e.SuppressKeyPress = true;
            }

            if (DotInField && Text.Substring(Text.IndexOf(',')).Length > MaxFract && !BackPressed)
            {
                e.SuppressKeyPress = true;
            }

            if (Text.Length > MaxTextLength && !BackPressed || e.KeyCode == Keys.Home ||
                e.KeyCode == Keys.End || e.KeyCode == Keys.Tab)
            {
                e.SuppressKeyPress = true;
            }

            if (DotPressed && Text.IndexOf('-') == 0 && SelectionStart == 1)
            {
                e.SuppressKeyPress = true;
            }

            if (DotPressed && Text == @"0" && TypeContent == ContentType.NumFloat)
            {
                Text = @"0,";
                SelectionStart = TextLength;
            }

            if (Text == @"-0" && ZeroPressed)
            {
                e.SuppressKeyPress = true;
            }

            if (TypeContent != ContentType.NumFloat && DotPressed)
            {
                e.SuppressKeyPress = true;
            }

            if (MinusPressed)
            {
                if (Text.IndexOf('-') < 0 && Text.IndexOf('0') != 0)
                {
                    Text = @"-" + Text;
                }
                else

                 if (Text.IndexOf('0') == 0)
                    Text = @"-";
                else
                {
                    Text = Text.Substring(1);
                }

                SelectionStart = TextLength;
            }
        }

        private void OmronEditKeyDown(object sender, KeyEventArgs e)
        {
            CheckInput(e);

            switch (e.KeyCode)
            {
                case Keys.Up:
                    FieldUp.Focus();
                    break;

                case Keys.Down:
                    FieldDown.Focus();
                    break;

                case Keys.Escape:
                    Text = @"0";
                    SelectionStart = 0;
                    BackColor = Color.Yellow;
                    break;

                case Keys.Enter:
                    BackColor = Color.LightGreen;
                   // SelectionStart = TextLength;
                    break;

                case Keys.Right:
                    ChangeValue(TypeContent, Operation.Increment);
                    break;

                case Keys.Left:
                    ChangeValue(TypeContent, Operation.Decrement);
                    break;
            }
        }

        private void ChangeValue(ContentType type, Operation op)
        {
            switch (type)
            {
                case ContentType.NumInt:

                    int i = int.Parse(Text);
                    if (op == Operation.Increment && i < MaxError)
                        i += (int)Step;
                    if (op == Operation.Decrement && i > MinError)
                        i -= (int)Step;
                    Text = i.ToString();
                    break;

                case ContentType.NumFloat:
                    if (Text.Contains("."))
                        Text = Text.Replace('.', ',');
                    float f = float.Parse(Text);//TODO Format Exception
                    if (op == Operation.Increment && f < MaxError)
                        f += Step;
                    if (op == Operation.Decrement && f > MinError)
                        f -= Step;
                    Text = f.ToString();
                    break;

                case ContentType.Automan:
                    break;

                case ContentType.OnOff:
                    break;

                case ContentType.Mode:
                    break;

                default:
                    MessageBox.Show(@"Error in ChangeValue method");
                    break;
            }
            SelectionStart = TextLength;
        }

        private void OmronEditLeave(object sender, EventArgs e)
        {
            if (PanelHeater.IsActive)
                BackColor = Color.DarkKhaki;
            if (Text == "" || Text == @"-")
                Text = @"0";
        }

        //private void SetFieldColor(TState state)
        //{
        //    switch (state)
        //    {
        //        case TState.Normal:
        //            break;
        //    }

        //}
    }
}