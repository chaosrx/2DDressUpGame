//----------------------------------------------
// UTAGE: Unity Text Adventure Game Engine
// Copyright 2014 Ryohei Tokimura
//----------------------------------------------

using UnityEngine;
using Utage;


namespace Utage
{

	/// <summary>
	/// 選択肢表示のサンプル
	/// </summary>
	[RequireComponent(typeof(LegacyUiListView))]
	[RequireComponent(typeof(Node2D))]
	[AddComponentMenu("Utage/Legacy/ADV/UiSelectionManager")]
	public class AdvUiSelectionManager : MonoBehaviour
	{
		/// <summary>ADVエンジン</summary>
		[SerializeField]
		AdvEngine engine;

		AdvSelectionManager SelectionManager { get { return engine.SelectionManager; } }

		bool isInit;

		/// <summary>選択肢のリストビュー</summary>
		public LegacyUiListView ListView
		{
			get { return listView ??(listView = GetComponent<LegacyUiListView>()); }
		}
		LegacyUiListView listView;


		/// <summary>
		/// 初期化。スクリプトから動的に生成する場合に
		/// </summary>
		/// <param name="engine">ADVエンジン</param>
		public void InitOnCreate(AdvEngine engine, AdvUiSelection selectionItemPrefab)
		{
			this.engine = engine;
			this.ListView.ItemPrefab = selectionItemPrefab.GetComponent<LegacyUiListViewItem>();
		}


		/// <summary>開く</summary>
		public void Open()
		{
			this.gameObject.SetActive(true);
		}

		/// <summary>閉じる</summary>
		public void Close()
		{
			ClearAll();
			this.gameObject.SetActive(false);
		}

		void Start()
		{
			ClearAll();
		}

		void Update()
		{
			//選択肢入力待ちなら、初期化して表示
			//そうでないなら非表示
			if (SelectionManager.IsWaitSelect)
			{
				if (!isInit)
				{
					Init();
				}
			}
			else
			{
				if (isInit) ClearAll();
			}
		}

		//全てクリア
		void ClearAll()
		{
			ListView.Close();
			isInit = false;
		}

		//初期化
		void Init()
		{
			ListView.Open(SelectionManager.Selections.Count, CallbackCreateItem);
			isInit = true;
		}

		//リストビューのアイテムが作成されるときに呼ばれるコールバック
		void CallbackCreateItem(GameObject go, int index)
		{
			AdvSelection data = SelectionManager.Selections[index];
			AdvUiSelection selection = go.GetComponentInChildren<AdvUiSelection>();
			selection.Init(data);
		}

		//選択肢が押された
		void OnTap(LegacyUiButton button)
		{
			SelectionManager.Select(button.Index);

			ClearAll();
		}
	}
}
