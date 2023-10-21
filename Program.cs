
using HerboldRacing;

namespace IRSDKSharperTest
{
	internal static class Program
	{
		public static IRSDKSharper IRSDKSharper { get; private set; }

		static Program()
		{
			IRSDKSharper = new IRSDKSharper();
		}
	}
}
