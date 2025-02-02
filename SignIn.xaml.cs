﻿// Pixeval
// Copyright (C) 2019 Dylech30th <decem0730@gmail.com>
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using Pixeval.Objects;
using Pixeval.Persisting;
using Refit;

namespace Pixeval
{
    public partial class SignIn
    {
        public SignIn()
        {
            InitializeComponent();
        }

        private async void SignIn_OnClosing(object sender, CancelEventArgs e)
        {
            if (Identity.Global.AccessToken == null)
            {
                PixevalEnvironment.LogoutExit = true;
                await Settings.Global.Store();
                Environment.Exit(0);
            }
        }

        private async void Login_OnClick(object sender, RoutedEventArgs e)
        {
            if (Email.Text.IsNullOrEmpty() || Password.Password.IsNullOrEmpty())
            {
                ErrorMessage.Content = Externally.EmptyEmailOrPasswordIsNotAllowed;
                return;
            }

            Login.Unable();

            try
            {
                await Authentication.Authenticate(Email.Text, Password.Password);
            }
            catch (Exception exception)
            {
                SetErrorHint(exception);
                Login.Enable();
                return;
            }

            var mainWindow = new MainWindow();
            mainWindow.Show();

            Close();
        }

        private async void SignIn_OnInitialized(object sender, EventArgs e)
        {
            if (Identity.ConfExists())
            {
                try
                {
                    DialogHost.IsOpen = true;
                    await Identity.RefreshIfRequired();
                }
                catch (ApiException exception)
                {
                    SetErrorHint(exception);

                    DialogHost.IsOpen = false;
                    return;
                }

                DialogHost.IsOpen = false;

                var mainWindow = new MainWindow();
                mainWindow.Show();
                Close();
            }
        }

        private async void SetErrorHint(Exception exception)
        {
            ErrorMessage.Content = exception is ApiException aException && await IsPasswordOrAccountError(aException)
                ? Externally.EmailOrPasswordIsWrong
                : exception.Message;
        }

        private static async ValueTask<bool> IsPasswordOrAccountError(ApiException exception)
        {
            var eMess = await exception.GetContentAsAsync<dynamic>();
            return eMess.errors.system.code == 1508;
        }
    }
}