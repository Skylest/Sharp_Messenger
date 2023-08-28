using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MessengerApp
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>

	public partial class MainWindow : Window
	{
		private HubConnection connection = new HubConnectionBuilder().WithUrl("https://localhost:7251/chathub").Build();
		private string username = "";
		private string nickname = "";

		public MainWindow()
		{
			InitializeComponent();
			Connect();
			onNewMessage();
		}

		private async Task Connect()
		{
			await connection.StartAsync();
		}

		//Обработчик нажатия на кнопку Login
		private void LoginButton_Click(object sender, RoutedEventArgs e)
		{
			connection.InvokeCoreAsync("Login", args: new[] { LoginBox.Text, PasswordBox.Password });
			connection.On("loginResult", (string message, bool result) =>
			{
				if (result)
				{
					//Если логин и пароль верные, то переходим на другой экран
					username = LoginBox.Text;
					contactListFill();
					Open(ContactsScreen);
				}
				else
				{
					//Иначе выводим сообщение об ошибке авторизации
					LoginMessageBlock.Text = message;
					LoginMessageBlock.Visibility = Visibility.Visible;
				}
			});
		}

		private void RegisterButton_Click(object sender, RoutedEventArgs e)
		{
			string error = "";
			bool errorsExist = CheckRegModel(RegisterBox.Text, PasswordRegBox.Password, PasswordConfirmBox.Password, NicknameBox.Text, out error);
			if (!errorsExist)
			{
				connection.InvokeCoreAsync("Register", args: new[] { RegisterBox.Text, PasswordRegBox.Password, NicknameBox.Text });
				connection.On("registerResult", (string message, bool result) =>
				{
					if (result)
					{
						//Если логин и пароль верные, то переходим на другой экран
						username = RegisterBox.Text;
						nickname = message;
						contactListFill();
						Open(ContactsScreen);
					}
					else
					{
						//Иначе выводим сообщение об ошибке авторизации
						RegisterMessageBlock.Text = message;
						RegisterMessageBlock.Visibility = Visibility.Visible;
					}
				});
			}
			else
			{				
				error.Remove(error.Length - 2, 2);
				RegisterMessageBlock.Text = error;
				RegisterMessageBlock.Visibility = Visibility.Visible;
			}
		}

		private bool CheckRegModel(string login, string password, string confirmPass, string nick, out string errorMessage)
		{
			bool flag = false;
			errorMessage = "";
			Regex regex = new Regex(@"^.*(?=.{8,})(?=.*\d)(?=.*[a-z])(?=.*[A-Z])(?=.*[!*@#$%^&+=]).*$");

			if (login.Length < 3)
			{
				errorMessage += "The login must be at least 3 characters.\n";
				flag = true;
			}
			if (password.Length < 7)
			{
				errorMessage += "The password must be at least 7 characters.\n";
				flag = true;
			}
			if (!regex.IsMatch(password))
			{
				errorMessage += "Must contain at least one lower case letter, one upper case letter, one digit and one special character.\n";
				flag = true;
			}
			if (password != confirmPass)
			{
				errorMessage += "The password and confirmation password do not match.\n";
				flag = true;
			}
			if (nick.Length < 3)
			{
				errorMessage += "The nickname must be at least 3 characters.\n";
				flag = true;
			}
			return flag;
		}

		//Метод для окрытия другого экрана
		private void Open(Border screen)
		{
			//Делаем все экраны невидимыми
			LoginScreen.Visibility = Visibility.Hidden;
			RegisterScreen.Visibility = Visibility.Hidden;
			ContactsScreen.Visibility = Visibility.Hidden;
			ChatScreen.Visibility = Visibility.Hidden;
			AddChannelScreen.Visibility = Visibility.Hidden;

			//Делаем видимым необходиый экран
			screen.Visibility = Visibility.Visible;
		}

		private void contactListFill()
		{
			connection.InvokeCoreAsync("getChannels", args: new[] { username });
			connection.On("channelsList", (List<ChannelViewModel> contacts) =>
			{
				ContactsList.Items.Clear();
				foreach (ChannelViewModel channel in contacts)
				{
					ContactsList.Items.Add(channel);
				}
			});

		}

		private void ContactsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			//Метод вызывается, когда меняется индекс выделенного элемента
			//При выделении элемент списка будет подсвечиваться
			//Чтобы убрать это, мы будем менять индекс на -1
			//Чтобы метод не срабатывал повторно, мы будем проверять, чтобы индекс был больше или равен 0
			if (ContactsList.SelectedIndex >= 0)
			{
				ChannelViewModel channelViewModel = ContactsList.SelectedItem as ChannelViewModel;
				ChatName.Text = channelViewModel.Name;
				connection.InvokeCoreAsync("GetMessageHistory", args: new[] { ChatName.Text });
				connection.On("chatHistory", (List<MessageView> messageViews) =>
				{
					MessagesList.Items.Clear();
					foreach (MessageView message in messageViews)
					{
						if (message.From == username)
						{
							message.Alignment = HorizontalAlignment.Right;
						}
						else message.Alignment = HorizontalAlignment.Left;
						MessagesList.Items.Add(message);
					}
				});
				ContactsList.SelectedIndex = -1;
				Open(ChatScreen);
			}

		}

		private void onNewMessage()
		{
			
			connection.On("newMessage", (MessageView message, string channelName) =>
			{
				if (channelName == ChatName.Text)
				{
					if (message.From == username)
					{
						message.Alignment = HorizontalAlignment.Right;
					}
					else message.Alignment = HorizontalAlignment.Left;
					MessagesList.Items.Add(message);
					MessagesList.SelectedIndex = MessagesList.Items.Count - 1;
					MessagesList.ScrollIntoView(MessagesList.SelectedItem);
				}
			});

			connection.On("addChannel", (ChannelViewModel view) =>
			{
				contactListFill();
				Open(ContactsScreen);
			});

		}

		private void SendButton_Click(object sender, RoutedEventArgs e)
		{
			string text = "";
			if (!string.IsNullOrEmpty(MessageBox.Text))
			{

				text = MessageBox.Text.Trim();
				connection.InvokeCoreAsync("Send", args: new[] { ChatName.Text, text, username });
				MessageBox.Text = "";				
			}
		}

		private void BackButton_Click(object sender, RoutedEventArgs e)
		{
			contactListFill();
			Open(ContactsScreen);
		}

		private void ShowCreateChannel_Click(object sender, RoutedEventArgs e)
		{
			Open(AddChannelScreen);

		}
		private void CreateChannel_Click(object sender, RoutedEventArgs e)
		{
			
			connection.InvokeCoreAsync("CreateChannel", args: new[] { ChannelNameBox.Text, username });
			connection.On("addChannel", (ChannelViewModel view) =>
			{
				contactListFill();
				Open(ContactsScreen);
			});			
		}	

		private void GoLoginButton_Click(object sender, RoutedEventArgs e)
		{
			LoginScreen.Visibility = Visibility.Visible;
			RegisterScreen.Visibility = Visibility.Hidden;
		}

		private void GoRegisterButton_Click(object sender, RoutedEventArgs e)
		{
			LoginScreen.Visibility = Visibility.Hidden;
			RegisterScreen.Visibility = Visibility.Visible;
		}
	}

	public class ChannelViewModel
	{
		public int Id { get; set; }
		public string Name { get; set; }
	}

	public class MessageView
	{
		public string Content { get; set; }
		public string Timestamp { get; set; }
		public string From { get; set; }
		public string To { get; set; }
		public HorizontalAlignment Alignment { get; set; }
	}
}
