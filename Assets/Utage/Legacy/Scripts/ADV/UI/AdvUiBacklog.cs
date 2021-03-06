//----------------------------------------------
// UTAGE: Unity Text Adventure Game Engine
// Copyright 2014 Ryohei Tokimura
//----------------------------------------------

using UnityEngine;
using Utage;



/// <summary>
/// バックログ用UI
/// </summary>
[RequireComponent(typeof(LegacyUiListViewItem))]
[AddComponentMenu("Utage/Legacy/ADV/UiBacklog")]
public class AdvUiBacklog : MonoBehaviour
{
	/// <summary>テキスト</summary>
	public TextArea2D text;

	/// <summary>キャラ名</summary>
	public TextArea2D characterName;

	/// <summary>ボイス再生アイコン</summary>
	public LegacyUiButton soundIcon;

	/// <summary>
	/// 初期化
	/// </summary>
	/// <param name="data">バックログのデータ</param>
	/// <param name="target">サウンドボタン押されたときのメッセージ送信先</param>
	/// <param name="index">バックログのインデックス</param>
	public void Init(AdvBacklog data, GameObject target, int index)
	{
		text.text = data.Text;
		characterName.text = data.CharacterName;
		soundIcon.Target = target;
		soundIcon.Index = index;
		if (!data.IsVoice)
		{
			soundIcon.gameObject.SetActive(false);
		}
	}
}
