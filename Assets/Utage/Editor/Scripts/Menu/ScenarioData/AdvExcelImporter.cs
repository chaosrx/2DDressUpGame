//----------------------------------------------
// UTAGE: Unity Text Adventure Game Engine
// Copyright 2014 Ryohei Tokimura
//----------------------------------------------
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace Utage
{

	//「Utage」のシナリオデータ用のエクセルファイルインポーター
	public class AdvExcelImporter : AssetPostprocessor
	{
		static void OnPostprocessAllAssets(
			string[] importedAssets,
			string[] deletedAssets,
			string[] movedAssets,
			string[] movedFromAssetPaths)
		{
			//制御エディタを通して、管理対象のデータのみインポートする
			AdvScenarioDataBuilderWindow.Import(importedAssets);
		}
		public const string ScenarioAssetExt = ".asset";
		public const string SettingAssetExt = " Setting.asset";		

		//シナリオデータ
		Dictionary<string, AdvScenarioData> scenarioDataTbl = new Dictionary<string, AdvScenarioData>();

		//ファイルの読み込み
		public bool Import(List<string> pathList)
		{
			//対象のエクセルファイルを全て読み込み
			Dictionary<string,StringGridDictionary> bookDictionary = new Dictionary<string,StringGridDictionary>();
			foreach (string path in pathList)
			{
				if (!string.IsNullOrEmpty(path))
				{
					StringGridDictionary book = ExcelParser.Read(path);
					if (book.List.Count > 0)
					{
						bookDictionary.Add(path, book);
					}
				}
			}

			if (bookDictionary.Count <= 0) return false;

			AdvEngine engine = UtageEditorToolKit.FindComponentAllInTheScene<AdvEngine>();
			if (engine != null)
			{
				engine.BootInitCustomCommand();
			}
			assetSetting = null;
			//設定データをインポート
			foreach (string path in bookDictionary.Keys)
			{
				ImportSettingBook(bookDictionary[path], path);
				if (assetSetting != null) break;
			}
			if (assetSetting == null) return false;

			AssetFileManager.IsEditorErrorCheck = true;
			AdvCommand.IsEditorErrorCheck = true;
			GraphicInfo.CallbackExpression = assetSetting.DefaultParam.CalcExpressionBoolean;
			TextParser.CallbackCalcExpression = assetSetting.DefaultParam.CalcExpressionNotSetParam;
			iTweenData.CallbackGetValue = assetSetting.DefaultParam.GetParameter;

			//シナリオデータをインポート
			foreach (string path in bookDictionary.Keys)
			{
				ImportScenarioBook(bookDictionary[path], path);
			}
			GraphicInfo.CallbackExpression = null;
			TextParser.CallbackCalcExpression = null;
			iTweenData.CallbackGetValue = null;

			//シナリオラベルのリンクチェック
			ErroeCheckScenarioLabel();

			AdvCommand.IsEditorErrorCheck = false;
			AssetFileManager.IsEditorErrorCheck = false;
			return true;
		}
		AdvSettingDataManager assetSetting;

		//設定データをインポート
		void ImportSettingBook(StringGridDictionary book, string path)
		{
			//インポート後のスクリプタブルオブジェクトを作成
			string assetPath = Path.GetDirectoryName(path) + "/" + Path.GetFileNameWithoutExtension(path) + SettingAssetExt;
			foreach (var sheet in book.List )
			{
				StringGrid grid = sheet.Grid;
				//設定データか、シナリオデータかチェック
				if (AdvSettingDataManager.IsBootSheet(sheet.Name) || AdvSettingDataManager.IsSettingsSheet(sheet.Name))
				{
					//設定データのアセットを作成
					if (assetSetting == null)
					{
						assetSetting = UtageEditorToolKit.GetImportedAssetCreateIfMissing<AdvSettingDataManager>(assetPath);
						assetSetting.Clear();
					}
					assetSetting.hideFlags = HideFlags.NotEditable;
					assetSetting.ParseFromExcel(sheet.Name, grid);
				}
			}

			if (assetSetting != null)
			{
				assetSetting.EditorTestInit();
				Debug.Log( LanguageAdvErrorMsg.LocalizeTextFormat(AdvErrorMsg.Import,assetPath));
				//変更を反映
				EditorUtility.SetDirty(assetSetting);
			}
		}

		//ブックのインポート
		void ImportScenarioBook(StringGridDictionary book, string path)
		{
			//シナリオデータ用のスクリプタブルオブジェクトを宣言
			string scenarioAssetPath = Path.ChangeExtension(path, ScenarioAssetExt);
			AdvScenarioDataExported assetScenario = null;

			foreach (var sheet in book.List)
			{
				StringGrid grid = sheet.Grid;
				//設定データか、シナリオデータかチェック
				if (!AdvSettingDataManager.IsBootSheet(sheet.Name) && !AdvSettingDataManager.IsSettingsSheet(sheet.Name))
				{
					//シナリオデータのアセットを作成
					if (assetScenario == null)
					{
						assetScenario = UtageEditorToolKit.GetImportedAssetCreateIfMissing<AdvScenarioDataExported>(scenarioAssetPath);
						assetScenario.Clear();
					}
					assetScenario.hideFlags = HideFlags.NotEditable;
					assetScenario.ParseFromExcel(sheet.Name, grid);
					if (assetSetting != null)
					{
						AdvScenarioData scenarioData = assetScenario.ErrorCheck(sheet.Name, grid, assetSetting);
						scenarioDataTbl.Add(sheet.Name, scenarioData);
					}
				}
			}

			//変更を反映
			if (assetScenario != null)
			{
				Debug.Log(LanguageAdvErrorMsg.LocalizeTextFormat(AdvErrorMsg.Import, scenarioAssetPath));
				EditorUtility.SetDirty(assetScenario);
			}
		}


		/// <summary>
		/// シナリオラベルのリンクチェック
		/// </summary>
		/// <param name="label">シナリオラベル</param>
		/// <returns>あればtrue。なければfalse</returns>
		void ErroeCheckScenarioLabel()
		{
			//リンク先のシナリオラベルがあるかチェック
			foreach (AdvScenarioData data in scenarioDataTbl.Values)
			{
				foreach (AdvScenarioJumpData jumpData in data.JumpScenarioData)
				{
					if (!IsExistScenarioLabel(jumpData.ToLabel))
					{
						Debug.LogError( 
							jumpData.FromRow.ToErrorString( 
							LanguageAdvErrorMsg.LocalizeTextFormat(AdvErrorMsg.NotLinkedScenarioLabel, jumpData.ToLabel, "")
							));
					}
				}
			}

			//シナリオラベルが重複しているかチェック
			foreach (AdvScenarioData data in scenarioDataTbl.Values)
			{
				foreach (AdvScenarioLabelData labelData in data.ScenarioLabelData)
				{
					if (IsExistScenarioLabel(labelData.ScenaioLabel, data))
					{
						string error = labelData.ToErrorString(LanguageAdvErrorMsg.LocalizeTextFormat(AdvErrorMsg.RedefinitionScenarioLabel, labelData.ScenaioLabel,""), data.DataGridName );
						Debug.LogError(error);
					}
				}
			}
		}


		/// <summary>
		/// シナリオラベルがあるかチェック
		/// </summary>
		/// <param name="label">シナリオラベル</param>
		/// <param name="egnoreData">チェックを無視するデータ</param>
		/// <returns>あればtrue。なければfalse</returns>
		bool IsExistScenarioLabel(string label, AdvScenarioData egnoreData = null )
		{
			foreach (AdvScenarioData data in scenarioDataTbl.Values)
			{
				if (data == egnoreData) continue;
				if (data.IsContainsScenarioLabel(label))
				{
					return true;
				}
			}
			return false;
		}
	}
}