using System.Security.Cryptography;
using System.Text;

namespace SenseLib.Utilities
{									
	public class Functions
	{
		public static int _UserID = 0;
		public static string _UserName = string.Empty;
		public static string _Message = string.Empty;
		public static string _Email = string.Empty;
		public static string _MessageEmail = string.Empty;
		public static int _Role = 0;

		public static string TitleSlugGeneration(string type, string title, long id)
		{
			string sTitle = type + "_" + SlugGenerator.SlugGenerator.GenerateSlug(title) + "-" + id.ToString() + ".html";
			return sTitle;
		}
		public static string ChangeStringToString(string title)
		{
			return SlugGenerator.SlugGenerator.GenerateSlug(title);

		}
		public static string MD5Hash(string text)
		{
			MD5 md5 = new MD5CryptoServiceProvider();
			md5.ComputeHash(Encoding.ASCII.GetBytes(text));
			byte[] result = md5.Hash;
			StringBuilder strBuilder = new StringBuilder();
			for (int i = 0; i < result.Length; i++)
			{
				strBuilder.Append(result[i].ToString("x2"));
			}
			return strBuilder.ToString();
		}
		public static string MD5Password(string? text)
		{
			string str = MD5Hash(text);
			//Lặp thêm 5 lần mã hóa xâu đảm bảo tính bảo mật 
			for (int i = 0; i <= 5; i++)

				str = MD5Hash(str + "_" + str);
			return str;
		}
		public static bool IsLogin()
		{
			if (string.IsNullOrEmpty(Functions._Email) || (Functions._UserID <= 0))
				return false;
			return true;
		}
	}
}
