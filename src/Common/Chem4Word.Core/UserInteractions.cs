// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Windows.Forms;

namespace Chem4Word.Core
{
    public static class UserInteractions
    {
        public static DialogResult AskUserYesNo(string message, MessageBoxDefaultButton defaultButton = MessageBoxDefaultButton.Button1)
        {
            return MessageBox.Show(message, CoreConstants.MessageBoxTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question, defaultButton);
        }

        public static DialogResult AskUserYesNoCancel(string message, MessageBoxDefaultButton defaultButton = MessageBoxDefaultButton.Button1)
        {
            return MessageBox.Show(message, CoreConstants.MessageBoxTitle, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, defaultButton);
        }

        public static DialogResult AskUserOkCancel(string message)
        {
            return MessageBox.Show(message, CoreConstants.MessageBoxTitle, MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
        }

        public static void InformUser(string message)
        {
            MessageBox.Show(message, CoreConstants.MessageBoxTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public static void AlertUser(string message)
        {
            MessageBox.Show(message, CoreConstants.MessageBoxTitle, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }

        public static void WarnUser(string message)
        {
            MessageBox.Show(message, CoreConstants.MessageBoxTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        public static void StopUser(string message)
        {
            MessageBox.Show(message, CoreConstants.MessageBoxTitle, MessageBoxButtons.OK, MessageBoxIcon.Stop);
        }
    }
}