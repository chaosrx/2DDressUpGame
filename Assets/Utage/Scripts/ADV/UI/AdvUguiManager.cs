//----------------------------------------------
// UTAGE: Unity Text Adventure Game Engine
// Copyright 2014 Ryohei Tokimura
//----------------------------------------------

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

namespace Utage
{

	/// <summary>
	/// UI全般の管理
	/// </summary>
    
	[AddComponentMenu("Utage/ADV/UiManager")]

	public class AdvUguiManager : AdvUiManager
	{
       // public GameObject utaka;
       // public Text message

        public Text msgtext;
        public Text text;
		public GameObject time_txt;
		public GameObject score_txt;
        public GameObject token;
		[SerializeField]
		AdvUguiMessageWindow messageWindow;

		[SerializeField]
		AdvUguiSelectionManager selection;

		[SerializeField]
		AdvUguiBacklogManager backLog;

		/// <summary>
		/// 初期化。スクリプトから動的に生成する場合に
		/// </summary>
		/// <param name="engine">ADVエンジン</param>
		/// 
		public void InitOnCreate(AdvEngine engine, AdvUguiMessageWindow messageWindow, AdvUguiSelectionManager selection, AdvUguiBacklogManager backLog)
		{
			this.engine = engine;
			this.messageWindow = messageWindow;
			this.selection = selection;
			this.backLog = backLog;
		}

		public override void Open()
		{
			ChangeStatus(UiStatus.Default);
			this.gameObject.SetActive(true);
		}

		public override void Close()
		{
			this.gameObject.SetActive(false);
			if (messageWindow != null) messageWindow.Close();
			if (selection != null) selection.Close();
			if (backLog != null) backLog.Close();
		}

		protected override void ChangeStatus(UiStatus newStatus)
		{
			switch (newStatus)
			{
				case UiStatus.Backlog:
					if (backLog == null) return;

					if (messageWindow != null) messageWindow.Close();
					if (selection != null) selection.Close();
					if (backLog != null) backLog.Open();
					engine.Config.IsSkip = false;
					break;
				case UiStatus.HideMessageWindow:
					if (messageWindow != null) messageWindow.Close();
					if (selection != null) selection.Close();
					if (backLog != null) backLog.Close();
					engine.Config.IsSkip = false;
					break;
				case UiStatus.Default:
					if (messageWindow != null) messageWindow.Open();
					if (selection != null) selection.Open();
					if (backLog != null) backLog.Close();
					break;
			}
			this.status = newStatus;
		}

		//ウインドウ閉じるボタンが押された
		void OnTapCloseWindow()
		{
			Status = UiStatus.HideMessageWindow;
		}

		IEnumerator wait()
		{
			yield return new WaitForSeconds (1.9f);
			time_txt.SetActive(true);
			score_txt.SetActive(true);
            token.SetActive(true);
		}
        IEnumerator dressup_wait()
        {
            yield return new WaitForSeconds(1.3f);
            Application.LoadLevel("Main");
        }
    void Update()
		{
      
        


            if (text.text == "Now pick your clothes")
            {
                StartCoroutine("dressup_wait");
                //	Debug.Log(true);

            }

			StartCoroutine ("wait");
			switch (engine.UiManager.Status)
			{
				case AdvUiManager.UiStatus.Backlog:
					break;
				case AdvUiManager.UiStatus.HideMessageWindow:	//メッセージウィンドウが非表示
					//右クリック
					if (InputUtil.IsMousceRightButtonDown())
					{	//通常画面に復帰
						engine.UiManager.Status = AdvUiManager.UiStatus.Default;
					}
					else if (InputUtil.IsInputScrollWheelUp())
					{
						//バックログ開く
						engine.UiManager.Status = AdvUiManager.UiStatus.Backlog;
					}
					break;
				case AdvUiManager.UiStatus.Default:
					if (engine.Page.IsWaitPage)
					{	//入力待ち
						if (InputUtil.IsMousceRightButtonDown())
						{	//右クリックでウィンドウ閉じる
							engine.UiManager.Status = AdvUiManager.UiStatus.HideMessageWindow;
						}
						else if (InputUtil.IsInputScrollWheelUp())
						{	//バックログ開く
							engine.UiManager.Status = AdvUiManager.UiStatus.Backlog;
						}
						else
						{
							if ( (engine.Config.IsMouseWheelSendMessage && InputUtil.IsInputScrollWheelDown())
								|| InputUtil.IsInputKeyboadReturnDown())
							{
								//メッセージ送り
								engine.Page.InputSendMessage();
							}
						}
					}
					break;
			}
		}

		/// <summary>
		/// タッチされたとき
		/// </summary>
		public void OnMouseDown(BaseEventData data)
		{

			if ((data as PointerEventData).button != PointerEventData.InputButton.Left) return;

			switch (engine.UiManager.Status)
			{
				case AdvUiManager.UiStatus.Backlog:
				//Debug.Log("a");
					break;
				case AdvUiManager.UiStatus.HideMessageWindow:	//メッセージウィンドウが非表示
					engine.UiManager.Status = AdvUiManager.UiStatus.Default;
				//Debug.Log("b");
					break;
				case AdvUiManager.UiStatus.Default:
					if (engine.Config.IsSkip)
				{
                        //Debug.Log("c");
					//スキップ中ならスキップ解除
						engine.Config.ToggleSkip();
					}
					else
					{
						if (engine.Page.IsShowingText)
						{
						time_txt.SetActive(true);
						score_txt.SetActive(true);
                        token.SetActive(true);


							if (!engine.Config.IsSkip)
							{
								//文字送り
							time_txt.SetActive(true);
							score_txt.SetActive(true);
                            token.SetActive(true);

							///Debug.Log("d");
						//	if(GameObject.FindWithTag("messagetext").GetComponent<UguiNovelText>().text=="Now pick your clothes")
                            
                            //{
							///	Application.LoadLevel("Main");
							//	Debug.Log(true);

						//	}

                            if (msgtext.text == "Now pick your clothes")
                            {
                                Application.LoadLevel("Main");
                                //	Debug.Log(true);

                            }
								engine.Page.InputSendMessage();
							}

						}
						else
						{
						//time_txt.SetActive(false);
							base.OnPointerDown(data as PointerEventData);
						}
					}
					break;
			}
		}
	}
}