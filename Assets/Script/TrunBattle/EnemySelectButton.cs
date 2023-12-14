using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySelectButton : MonoBehaviour
{
	public GameObject enemyPrefab;

	public void SelectEnemy()
	{
		GameObject.Find("BattleManager").GetComponent<BattleStateMaschine>().Input2(enemyPrefab);
	}
	 
	public void HideSelector()
	{
			enemyPrefab.transform.Find("Selector").gameObject.SetActive(false);
	}	
	public void ShowSelector()
	{
			enemyPrefab.transform.Find("Selector").gameObject.SetActive(true);
	}
}
