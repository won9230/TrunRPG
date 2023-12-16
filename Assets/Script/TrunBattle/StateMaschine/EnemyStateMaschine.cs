using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnemyStateMaschine : MonoBehaviour
{
	public BaseEnemy enemy;
	public BattleStateMaschine BSM;
	public enum TurnState
	{
		Processing,
		ChooseAction,
		Waiting,
		Action,
		Dead
	}

	public TurnState currentState;

	private float cur_cooldown = 0f;
	private float max_cooldown = 10f;	//10f

	private Vector3 startPosition;

	private bool actionStarted = false;
	public GameObject heroToAttack;
	private float animSpeed = 10f;
	public GameObject select;

	private void Start()
	{
		currentState = TurnState.Processing;
		select.SetActive(false);
		BSM = GameObject.Find("BattleManager").GetComponent<BattleStateMaschine>();
		startPosition = transform.position;
	}

	private void Update()
	{
		switch (currentState)
		{
			case TurnState.Processing:
				UpgradeProgressBar();
				break;
			case TurnState.ChooseAction:
				ChooseAction();
				currentState = TurnState.Waiting;
				break;
			case TurnState.Waiting:
				//idle state
				break;
			case TurnState.Action:
				StartCoroutine(TimeForAction());
				break;
			case TurnState.Dead:
				break;
			default:
				break;
		}


	}

	//�ൿ ���� üũ
	private void UpgradeProgressBar()
	{
		cur_cooldown = cur_cooldown + Time.deltaTime;


		if (cur_cooldown >= max_cooldown)
		{
			currentState = TurnState.ChooseAction;
		}
	}

	//������ ������Ʈ�� ���Ѵ�.
	private void ChooseAction()
	{
		HandleTrun myAttack = new HandleTrun();
		myAttack.attacker = enemy.theName;
		myAttack.Type = "Enemy";
		myAttack.attackersGamgeObject = this.gameObject;
		myAttack.attackersTarget = BSM.heroInBattle[Random.Range(0, BSM.heroInBattle.Count)];

		int num = Random.Range(0, enemy.attacks.Count);
		myAttack.choosenAttack = enemy.attacks[num];
		Debug.Log(this.gameObject.name + " has choosen " + myAttack.choosenAttack.attackName + " and do " + myAttack.choosenAttack.attackDamage + " damage!");

		BSM.CollectActions(myAttack);
	}

	private IEnumerator TimeForAction()
	{
		if (actionStarted)
		{
			yield break;
		}
		actionStarted = true;
		//���� ��ó���� ���� �ִϸ��̼�
		Vector3 heroPosition = new Vector3(heroToAttack.transform.position.x, heroToAttack.transform.position.y,heroToAttack.transform.position.z + 1.5f);

		while (MoveTowardsEnemy(heroPosition))
		{
			yield return null;
		}

		//���
		yield return new WaitForSeconds(0.5f);
		//�����
		DoDamage();
		//������ġ�� ����
		Vector3 firstPosition = startPosition;
		while (MoveTowardsStart(firstPosition))
		{
			yield return null;
		}
		//BSM���� performer����
		BSM.performList.RemoveAt(0);
		//BSM�� Wait���� ����
		BSM.battleState = BattleStateMaschine.PerformAction.Wait;

		actionStarted = false;
		//�� ���� �ʱ�ȭ
		cur_cooldown = 0f;
		currentState = TurnState.Processing;
	}

	//�÷��̾ ������ �̵�
	private bool MoveTowardsEnemy(Vector3 target)
	{
		//������ true
		return target != (transform.position = Vector3.MoveTowards(transform.position,target,animSpeed * Time.deltaTime));
	}
	//�÷��̾ �ڱ� �ڸ��� �̵�
	private bool MoveTowardsStart(Vector3 target)
	{
		//������ true
		return target != (transform.position = Vector3.MoveTowards(transform.position, target, animSpeed * Time.deltaTime));
	}

	private void DoDamage()
	{
		float calc_damage = enemy.curATK + BSM.performList[0].choosenAttack.attackDamage;
		heroToAttack.GetComponent<HeroStateMaschine>().TakeDamage(calc_damage);
	}

	public void TakeDamage(float getDamageAmount)
	{
		enemy.curHp -= getDamageAmount;
		if(enemy.curHp <= 0)
		{
			enemy.curHp = 0;
			currentState = TurnState.Dead;
		}
	}
}