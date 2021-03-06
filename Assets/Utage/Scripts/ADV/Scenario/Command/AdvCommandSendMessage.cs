//----------------------------------------------
// UTAGE: Unity Text Adventure Game Engine
// Copyright 2014 Ryohei Tokimura
//----------------------------------------------
using System;

namespace Utage
{

	/// <summary>
	/// コマンド：ゲーム固有の独自処理のためにSendMessageをする
	/// </summary>
	public class AdvCommandSendMessage : AdvCommand
	{
		public AdvCommandSendMessage(StringGridRow row)
			: base(row)
		{
			this.name = AdvParser.ParseCell<string>(row, AdvColumnName.Arg1);
			this.arg2 = AdvParser.ParseCellOptional<string>(row, AdvColumnName.Arg2, "");
			this.arg3 = AdvParser.ParseCellOptional<string>(row, AdvColumnName.Arg3, "");
			this.arg4 = AdvParser.ParseCellOptional<string>(row, AdvColumnName.Arg4, "");
			this.arg5 = AdvParser.ParseCellOptional<string>(row, AdvColumnName.Arg5, "");

			this.text = AdvParser.ParseCellOptional<string>(row, AdvColumnName.Text, "");
			this.voice = AdvParser.ParseCellOptional<string>(row, AdvColumnName.Voice, "");
			this.voiceVersion = AdvParser.ParseCellOptional<int>(row, AdvColumnName.VoiceVersion, 0);
		}

		public override void DoCommand(AdvEngine engine)
		{
			UtageToolKit.SafeSendMessage( this, engine.ScenarioPlayer.SendMessageTarget, "OnDoCommand" );
		}

		public override bool Wait(AdvEngine engine)
		{
			UtageToolKit.SafeSendMessage(this, engine.ScenarioPlayer.SendMessageTarget, "OnWait");
			return IsWait;
		}

		/// <summary>
		/// コマンドの待機処理をするか
		/// </summary>
		public bool IsWait { get { return isWait; } set { isWait = value; } }
		bool isWait = false;

		/// <summary>
		/// 名前
		/// </summary>
		public string Name { get { return name; } }
		string name;

		/// <summary>
		/// 引数2
		/// </summary>
		public string Arg2 { get { return arg2; } }
		string arg2;

		/// <summary>
		/// 引数3
		/// </summary>
		public string Arg3 { get { return arg3; } }
		string arg3;

		/// <summary>
		/// 引数4
		/// </summary>
		public string Arg4 { get { return arg4; } }
		string arg4;

		/// <summary>
		/// 引数5
		/// </summary>
		public string Arg5 { get { return arg5; } }
		string arg5;

		/// <summary>
		/// テキスト
		/// </summary>
		public string Text { get { return text; } }
		string text;

		/// <summary>
		/// ボイス
		/// </summary>
		public string Voice { get { return voice; } }
		string voice;


		/// <summary>
		/// ボイスバージョン
		/// </summary>
		public int VoiceVersion { get { return voiceVersion; } }
		int voiceVersion;
	}
}