//----------------------------------------------
// UTAGE: Unity Text Adventure Game Engine
// Copyright 2014 Ryohei Tokimura
//----------------------------------------------

using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Utage
{

	/// <summary>
	/// シナリオを進めていくプレイヤー
	/// </summary>
	[AddComponentMenu("Utage/ADV/Internal/ScenarioPlayer")]
	public class AdvScenarioPlayer : MonoBehaviour
	{
		/// <summary>
		/// 「SendMessage」コマンドが実行されたときにSendMessageを受け取るGameObject
		/// </summary>
		public GameObject SendMessageTarget { get { return sendMessageTarget; } }
		[SerializeField]
		GameObject sendMessageTarget;

		[System.Flags]
		enum DebugOutPut
		{
			Log = 0x01,
			Waiting = 0x02,
			CommandEnd = 0x04,
		};
		[SerializeField]
		[EnumFlags]
		DebugOutPut debugOutPut = 0;

	
		/// <summary>
		/// 現在の、シーン回想用のシーンラベル
		/// </summary>
		public string CurrentGallerySceneLabel { get { return this.currentGallerySceneLabel;}  set{ this.currentGallerySceneLabel = value;} }
		string currentGallerySceneLabel = "";


		/// <summary>
		/// ロード中か
		/// </summary>
		public bool IsWaitLoading { get { return this.isWaitLoading ; } }
		bool isWaitLoading = false;

		/// <summary>
		/// シナリオ終了したか
		/// </summary>
		public bool IsEndScenario { get { return this.isEndScenario; } }
		bool isEndScenario = false;


		//If文制御のマネージャー
		internal AdvIfManager IfManager { get { return this.ifManager; } }
		AdvIfManager ifManager = new AdvIfManager();

		//ジャンプのマネージャー
		internal AdvJumpManager JumpManager { get { return this.jumpManager; } }
		AdvJumpManager jumpManager = new AdvJumpManager();

		[SerializeField] int maxFilePreload = 20;	///事前にロードするファイルの最大数
		HashSet<AssetFile> preloadFileSet = new HashSet<AssetFile>();

		AdvEngine Engine { get { return this.engine ?? (this.engine = GetComponent<AdvEngine>()); } }
		AdvEngine engine;

		/// <summary>
		/// 古いバージョンのセーブデータか
		/// </summary>
		public bool IsOldVersion
		{ 
			get { return this.isOldVersion; }
			set { this.isOldVersion = value; }
		}
		bool isOldVersion = false;

		bool IsBreakCommand
		{
			get { return JumpManager.IsLabeled || IsReservedEndScenario;}
		}

		//
		bool IsReservedEndScenario {
			get { return isReservedEndScenario;}
		}
		bool isReservedEndScenario;
		public void ReserveEndScenario()
		{
			isReservedEndScenario = true;
		}

		/// <summary>
		/// 最初の状態に
		/// </summary>
		void Reset()
		{
			isReservedEndScenario = false;
			isWaitLoading = false;
			ifManager.Clear();
			jumpManager.Clear();
			ClearPreload();
		}

		/// <summary>
		/// シナリオの実行開始
		/// </summary>
		/// <param name="engine">ADVエンジン</param>
		/// <param name="scenarioLabel">ジャンプ先のシナリオラベル</param>
		/// <param name="page">シナリオラベルからのページ数</param>
		/// <param name="scenarioLabel">ジャンプ先のシーン回想用シナリオラベル</param>
		public void StartScenario(string label, int page, string gallerySceneLabel)
		{
			this.isEndScenario = false;
			//セーブデータからロードする時用に現在のシーン回想登録用のラベルを記録
			this.currentGallerySceneLabel = gallerySceneLabel;

			//前回の実行がまだ回ってるかもしれないので止める
			StopAllCoroutines();
			StartCoroutine( CoStartScenario(label, page));
		}
		
		/// <summary>
		/// シナリオ終了
		/// </summary>
		public void EndScenario()
		{
			Engine.Clear();
			isEndScenario = true;
		}

		/// <summary>
		/// シナリオ終了
		/// </summary>
		public void Clear()
		{
			Reset();
			StopAllCoroutines();
		}

		void JumpToLabelReserved()
		{
			//前回の実行がまだ回ってるかもしれないので止める
			StopAllCoroutines();
			StartCoroutine(CoStartScenario( JumpManager.Label, 0));
		}

		//指定のシナリオを再生
		IEnumerator CoStartScenario( string label, int page)
		{
			//ジャンプ先のシナリオラベルのログを出力
			if ((debugOutPut & DebugOutPut.Log) == DebugOutPut.Log) Debug.Log("Jump : " + label + " :" + page);

			//ジャンプ直後は1フレーム遅らせないと、ジャンプコマンドが正常に動作しない
//			yield return 0;
			
			//起動時のロード待ち
			while (Engine.IsLoading)
			{
				yield return 0;
			}


			//シナリオロード待ち
			isWaitLoading = true;
			while (!Engine.DataManager.IsLoadEndScenarioLabel(label))
			{
				yield return 0;
			}
			isWaitLoading = false;

			//各データをリセット
			Reset();

			if (page < 0) page = 0;
			//ページ指定がある場合はif分岐の設定をしておく
			if (page != 0) ifManager.IsLoadInit = true;

			//ジャンプ先のシナリオデータを取得
			AdvScenarioLabelData currentLabelData = Engine.DataManager.FindScenarioLabelData(label);
			while (currentLabelData!=null)
			{
				UpdateSceneGallery(currentLabelData.ScenaioLabel, engine);
				AdvScenarioPageData cuurentPageData = currentLabelData.GetPageData(page);
				//ページデータを取得
				while (cuurentPageData != null)
				{
					//プリロードを更新
					UpdatePreLoadFiles(currentLabelData.ScenaioLabel, page);

					///ページ開始処理
					Engine.Page.BeginPage(currentLabelData.ScenaioLabel, page);
					Engine.SaveManager.UpdateAutoSaveData(engine);

					var pageCoroutine = StartCoroutine(CoStartPage(currentLabelData, cuurentPageData, page));
					if (pageCoroutine != null)
					{
						yield return pageCoroutine;
					}
					while(Engine.EffectManager.IsPageWaiting ) yield return 0;

					//古いバージョンのロード処理は終了
					IsOldVersion = false;

					///改ページ処理
					if (true)
					{
						//ボイスを止める
						if (Engine.Config.VoiceStopType == VoiceStopType.OnClick)
						{
							Engine.SoundManager.StopVoice();
						}

						Engine.SystemSaveData.ReadData.AddReadPage(engine.Page.ScenarioLabel, page);
						Engine.Page.EndPage();
					}
					if(IsBreakCommand)
					{
						if( IsReservedEndScenario)
						{
							break;
						}
						if( JumpManager.IsLabeled ) JumpToLabelReserved();
						yield break;
					}

					cuurentPageData = currentLabelData.GetPageData(++page);
				}
				if (IsReservedEndScenario)
				{
					break;
				}
				//ロード直後処理終了
				IfManager.IsLoadInit = false;
				currentLabelData = Engine.DataManager.NextScenarioLabelData(currentLabelData.ScenaioLabel);
				page = 0;
			}
			EndScenario();
		}

		//一ページ内のコマンド処理
		IEnumerator CoStartPage( AdvScenarioLabelData labelData,  AdvScenarioPageData pageData, int page)
		{
			int index = 0;
			AdvCommand command = pageData.GetCommand(index);
			while (command!=null)
			{
				//古いセーブデータのロード中はページ末までスキップ
				if (IsOldVersion && !command.IsTypePageEnd())
				{
					command = pageData.GetCommand(++index);
					continue;
				}

				//ifスキップチェック
				if(IfManager.CheckSkip(command))
				{
					if ((debugOutPut & DebugOutPut.Log) == DebugOutPut.Log) Debug.Log("Command If Skip: " + command.GetType() + " " + labelData.ScenaioLabel + ":" + page);
					command = pageData.GetCommand(++index);
					continue;
				}

				//ロード
				command.Load();

				//ロード待ち
				while (!command.IsLoadEnd())
				{
					isWaitLoading = true;
					yield return 0;
				}
				isWaitLoading = false;

				//コマンド実行
				if ((debugOutPut & DebugOutPut.Log) == DebugOutPut.Log) Debug.Log("Command : " + command.GetType() + " " + labelData.ScenaioLabel + ":" + page);
				command.DoCommand(engine);
				///ページ末端・オートセーブデータを更新
//				if (command.IsTypePageEnd())
//				{
//					///ページ開始処理
//					engine.Page.BeginPage(currentScenarioLabel, currentPage);
//					engine.SaveManager.UpdateAutoSaveData(engine);
//				}

				//コマンド実行後にファイルをアンロード
				command.Unload();

				//コマンドの処理待ち
				while (command.Wait(engine))
				{
					if ((debugOutPut & DebugOutPut.Waiting) == DebugOutPut.Waiting) Debug.Log("Wait..." + command.GetType());
					yield return 0;
				}

				if ((debugOutPut & DebugOutPut.CommandEnd) == DebugOutPut.CommandEnd) Debug.Log("End :" + command.GetType() + " " + labelData.ScenaioLabel+ ":" + page);

				if(IsBreakCommand)
				{
					yield break;
				}
				command = pageData.GetCommand(++index);
			}
		}

		//先読みファイルをクリア
		void ClearPreload()
		{
			//直前の先読みファイルは参照を減算しておく
			foreach (AssetFile file in preloadFileSet)
			{
				file.Unuse(this);
			}
			preloadFileSet.Clear();
		}

		//先読みかけておく
		void UpdatePreLoadFiles(string scenarioLabel, int page)
		{
			//直前までの先読みファイルリスト
			HashSet<AssetFile> lastPreloadFileSet = preloadFileSet;
			//今回の先読みファイルリスト
			preloadFileSet = Engine.DataManager.MakePreloadFileList(scenarioLabel, page, maxFilePreload);

			if (preloadFileSet == null) preloadFileSet = new HashSet<AssetFile>();

			//リストに従って先読み
			foreach (AssetFile file in preloadFileSet)
			{
				//先読み
				AssetFileManager.Preload(file, this);
			}

			//直前の先読みファイルのうち、今回の先読みファイルからなくなったものは使用状態を解除する
			foreach (AssetFile file in lastPreloadFileSet)
			{
				//もうプリロードされなくなったリストを作るために
				if (!(preloadFileSet.Contains(file)))
				{
					file.Unuse(this);
				}
			}
		}


		/// <summary>
		/// シーン回想のためにシーンラベルを更新
		/// </summary>
		/// <param name="label">シーンラベル</param>
		/// <param name="engine">ADVエンジン</param>
		void UpdateSceneGallery(string label, AdvEngine engine)
		{
			AdvSceneGallerySetting galleryData = engine.DataManager.SettingDataManager.SceneGallerySetting;
			if (galleryData.ContainsKey(label))
			{
				if (currentGallerySceneLabel != label)
				{
					if (!string.IsNullOrEmpty(currentGallerySceneLabel))
					{
						//別のシーンが終わってないのに、新しいシーンに移っている
						Debug.LogError(LanguageAdvErrorMsg.LocalizeTextFormat(AdvErrorMsg.UpdateSceneLabel, currentGallerySceneLabel, label));
					}
					currentGallerySceneLabel = label;
				}
			}
		}

		/// <summary>
		/// シーン回想のためのシーンの終了処理
		/// </summary>
		/// <param name="engine">ADVエンジン</param>
		public void EndSceneGallery(AdvEngine engine)
		{
			if (string.IsNullOrEmpty(currentGallerySceneLabel))
			{
				//シーン回想に登録されていないのに、シーン回想終了がされています
				Debug.LogError(LanguageAdvErrorMsg.LocalizeTextFormat(AdvErrorMsg.EndSceneGallery));
			}
			else
			{
				engine.SystemSaveData.GalleryData.AddSceneLabel(currentGallerySceneLabel);
				currentGallerySceneLabel = "";
			}
		}
	}
}