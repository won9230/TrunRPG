using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static BattleStateMaschine;

public class EnemyStateMaschine : MonoBehaviour
{
	public BaseEnemy enemy;
	[HideInInspector] BattleStateMaschine BSM;
	[HideInInspector] public AnimatorManager anim;
	//hp바
	public Slider enemyHpBarSlider;
	public GameObject enemyHpBar;
	private Transform sliderFill_Area;  //hp바 Fill_Area

	public enum TurnState
	{
		Processing,
		ChooseAction,
		Waiting,
		Action,
		Dead
	}

	public TurnState currentState;
	
	private Vector3 startPosition;

	private bool actionStarted = false;
	private float animSpeed = 10f;
	public GameObject heroToAttack;
	public GameObject select;

	//생존여부
	private bool alive = true;

	private void Awake()
	{
		BSM = GameObject.Find("BattleManager").GetComponent<BattleStateMaschine>();
		anim = GetComponent<AnimatorManager>();
		enemyHpBar = transform.Find("Hp Bar Canvas").transform.Find("Slider").gameObject;
		enemyHpBarSlider = enemyHpBar.GetComponent<Slider>();
		sliderFill_Area = enemyHpBar.transform.Find("Fill Area");
		
	}

	private void Start()
	{
		Error_checking();
		currentState = TurnState.Processing;
		select.SetActive(false);
		enemyHpBar.SetActive(false);

		startPosition = this.transform.position;

		enemyHpBarSlider.maxValue = enemy.baseHp;
		enemyHpBarSlider.minValue = 0;
		enemyHpBarSlider.value = enemy.curHp;

	}

	//오류 검사
	private void Error_checking()
	{
		if (BSM == null)
			Debug.LogError("BSM이 없습니다.");
		if (anim == null)
			Debug.LogError("AnimatorManager가 없습니다");
		if (enemyHpBarSlider == null)
			Debug.LogError("enemyHpBarSlider 없습니다");
		if (sliderFill_Area == null)
			Debug.LogError("sliderFill_Area가 없습니다");
	}

	private void Update()
	{
		//Debug.Log(this.name + " " + currentState);
		switch (currentState)
		{
			case TurnState.Processing:
				UpgradeProgressBar();
				break;
			case TurnState.ChooseAction:
				if(BSM.heroInBattle.Count != 0)
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
				if (!alive)
				{
					return;
				}
				else
				{
					//enemy로 태그 변경
					this.gameObject.tag = "DeadEnemy";
					//영웅을 공격하지 않음
					BSM.enemyInBattle.Remove(this.gameObject);
					//selector 비활성화
					select.SetActive(false);
					//리스트에서 삭제
					RemoveAttackersTarget();
					RemoveBattleOrder();
					alive = false;
					//적 선택 버튼 초기화
					BSM.EnemyButtons();
					//생존 체크
					BSM.battleState = PerformAction.checkAlive;
				}
				break;
			default:
				break;
		}


	}

	//행동 가능 체크
	private void UpgradeProgressBar()
	{
		if (BSM.battleOrders[0].attackerName == this.name && !BSM.isEnemyAttack)
		{
			BSM.isEnemyAttack = true;
            currentState = TurnState.ChooseAction;
			StartCoroutine(WaitTime(0.1f));
		}
	}

	//공격할 오브젝트를 정한다.
	private void ChooseAction()
	{
		HandleTrun myAttack = new HandleTrun();
		myAttack.attacker = this.name;
		myAttack.Type = "Enemy";
		myAttack.attackersGamgeObject = this.gameObject;
		myAttack.attackersTarget = BSM.heroInBattle[Random.Range(0, BSM.heroInBattle.Count)];

		int num = Random.Range(0, enemy.attacks.Count);
		myAttack.choosenAttack = enemy.attacks[num];
		//Debug.Log(this.gameObject.name + " has choosen " + myAttack.choosenAttack.attackName + " and do " + myAttack.choosenAttack.attackDamage + " damage!");

		BSM.perform = myAttack;
    }
	
	//공격 동작
	private IEnumerator TimeForAction()
	{
		BSM.UIPanel.SetActive(false);
		if (actionStarted)
		{
			yield break;
		}
		actionStarted = true;
		//영웅 근처에서 공격 애니메이션
		Vector3 heroPosition = new Vector3(heroToAttack.transform.position.x, heroToAttack.transform.position.y,heroToAttack.transform.position.z + 1.5f);
		//이동 애니메이션
		anim.RunAnim(true);
		while (MoveTowardsEnemy(heroPosition))
		{
			yield return null;
		}
		//공격 애니메이션
		anim.AttackAnim(true);
		//대기
		yield return new WaitForSeconds(0.01f);
		//Debug.Log(anim.GetAnimTime());
		yield return new WaitForSeconds(anim.GetAnimTime() + 0.1f);
		
		DoDamage();
		//원래위치로 복귀
		//대미지 
		anim.AttackAnim(false);
		while (MoveTowardsStart(startPosition))
		{
			yield return null;
		}
		anim.RunAnim(false);
		//BSM에서 performer제거
		BSM.perform = new HandleTrun();
		//BSM를 Wait으로 변경
		BSM.battleState = PerformAction.Wait;
		actionStarted = false;
		//적 상태 초기화
		//cur_cooldown = 0f;
		BSM.BattleNext();
		BSM.isEnemyAttack = false;

		currentState = TurnState.Processing;
	}

	//플레이어가 적에게 이동
	private bool MoveTowardsEnemy(Vector3 target)
	{
		//성공시 true
		return target != (transform.position = Vector3.MoveTowards(transform.position,target,animSpeed * Time.deltaTime));
	}
	//플레이어가 자기 자리로 이동
	private bool MoveTowardsStart(Vector3 target)
	{
		//성공시 true
		return target != (transform.position = Vector3.MoveTowards(transform.position, target, animSpeed * Time.deltaTime));
	}

	//데미지 입힘
	private void DoDamage()
	{
		float calc_damage = enemy.curATK + BSM.perform.choosenAttack.attackDamage;
		heroToAttack.GetComponent<HeroStateMaschine>().TakeDamage(calc_damage);
	}

	//데미지 입음
	public void TakeDamage(float getDamageAmount)
	{
		enemy.curHp -= getDamageAmount;
		
		if(enemy.curHp <= 0)
		{
			sliderFill_Area.gameObject.SetActive(false);
			anim.DieAnim(true);
			enemy.curHp = 0;
			currentState = TurnState.Dead;
		}
		else
		{
			enemyHpBarSlider.value = enemy.curHp;
			//Debug.Log("적 데미지");
			anim.TakeDamageAnim();
			StartCoroutine(WaitTime(0.01f));
			StartCoroutine(WaitTime(anim.GetAnimTime()));
		}

	}

	//미리 들어가 있는 공격을 삭제
	private void RemoveAttackersTarget()
	{
		if (BSM.enemyInBattle.Count > 0)
		{
			if (BSM.perform.attackersGamgeObject == this.gameObject)
			{
				BSM.perform = null;
			}
			if (BSM.perform.attackersTarget == this.gameObject)
			{
				BSM.perform.attackersTarget = BSM.enemyInBattle[Random.Range(0, BSM.enemyInBattle.Count)];

			}
		}
	}

	//배틀 순서에서 삭제
	private void RemoveBattleOrder()
	{
		if (BSM.battleOrders.Count > 0)
		{
			for (int i = 0; i < BSM.battleOrders.Count; i++)
			{
				if (BSM.battleOrders[i].attackerName == this.gameObject.name)
				{
					BSM.battleOrders.Remove(BSM.battleOrders[i]);
					return;
				}
			}
		}
	}

	//대기 코루틴
	private IEnumerator WaitTime(float _time)
	{
		yield return new WaitForSeconds(_time);
	}
}
