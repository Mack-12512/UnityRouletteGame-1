using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class BattleManager : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text playerHPText;
    public TMP_Text enemyHPText;
    public TMP_Text turnText;
    public TMP_Text timerText;

    [Header("Damage UI")]
    public TMP_Text playerDamageText;
    public TMP_Text enemyDamageText;
    public TMP_Text resultText;

    [Header("Roulette")]
    public TMP_Text[] playerSlots;
    public TMP_Text[] enemySlots;

    [Header("Buttons")]
    public Button rouletteButton;
    public Button[] itemButtons;

    [Header("Item Text")]
    public TMP_Text[] itemTexts;

    [Header("Roulette Sound")]
    public AudioSource audioSource;
    public AudioClip slotStopSound;
    public AudioClip sameNumberSound;

    [Header("HP Effect Sound")]
    public AudioClip damageSound;
    public AudioClip healSound;

    [Header("Item Sound")]
    public AudioClip itemUseSound;

    [Header("Roulette Settings")]
    public float spinDuration = 1.5f;
    public float slotStopInterval = 0.35f;
    public float numberChangeSpeed = 0.05f;

    [Header("Slot Animation")]
    public float popScale = 1.4f;
    public float popSpeed = 12f;

    [Header("HP Effect")]
    public float hpEffectScale = 1.3f;
    public float hpEffectSpeed = 12f;

    [Header("Battle")]
    public int playerHP;
    public int enemyHP;

    private int turn = 1;
    private float timer = 300f;

    private bool battleEnded = false;
    private bool battleBusy = false;

    private Color normalSlotColor = Color.white;
    private Color sameNumberColor = Color.yellow;

    // 난이도
    private int difficulty;
    private float playerDamageMultiplier = 1f;
    private float enemyDamageMultiplier = 1f;

    // 아이템
    private string[] items = new string[6];

    [Header("Item Description")]
    public SkillDescription descriptionUI;

    void Start()
    {
        difficulty = PlayerPrefs.GetInt("Difficulty", 2);
        ApplyDifficulty();

        playerDamageText.text = "내 공격: -";
        enemyDamageText.text = "상대 공격: -";
        resultText.text = "룰렛을 돌리세요!";

        for (int i = 0; i < 6; i++)
            items[i] = "";

        UpdateItemUI();
        ResetSlotColors();

        UpdateUI();
        SetButtons(true);
    }

    void ApplyDifficulty()
    {
        switch (difficulty)
        {
            // 쉬움
            case 0:
                playerHP = 200;
                enemyHP = 80;
                playerDamageMultiplier = 1.5f;
                enemyDamageMultiplier = 0.5f;
                break;

            // 중간
            case 1:
                playerHP = Random.Range(120, 201);
                enemyHP = Random.Range(120, 201);
                playerDamageMultiplier = 1f;
                enemyDamageMultiplier = 0.8f;
                break;

            // 어려움
            case 2:
                playerHP = Random.Range(120, 301);
                enemyHP = Random.Range(200, 401);
                playerDamageMultiplier = 1f;
                enemyDamageMultiplier = 1.2f;
                break;

            default:
                playerHP = 120;
                enemyHP = 120;
                playerDamageMultiplier = 1f;
                enemyDamageMultiplier = 1f;
                break;
        }
    }

    void Update()
    {
        if (battleEnded)
            return;

        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            timer = 0f;
            EndBattle();
        }

        int minutes = Mathf.FloorToInt(timer / 60f);
        int seconds = Mathf.FloorToInt(timer % 60f);

        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    // =========================
    // 룰렛
    // =========================

    public void SpinRoulette()
    {
        if (battleEnded || battleBusy)
            return;

        StartCoroutine(RouletteSequence());
    }

    IEnumerator RouletteSequence()
    {
        battleBusy = true;
        SetButtons(false);

        TMP_Text rouletteText =
            rouletteButton.GetComponentInChildren<TMP_Text>();

        if (rouletteText != null)
            rouletteText.text = "돌리는중...";

        resultText.text = "룰렛을 돌리는 중...";
        ResetSlotColors();

        Coroutine[] playerSpin = new Coroutine[5];
        Coroutine[] enemySpin = new Coroutine[5];

        for (int i = 0; i < 5; i++)
        {
            playerSpin[i] = StartCoroutine(SpinSlot(playerSlots[i]));
            enemySpin[i] = StartCoroutine(SpinSlot(enemySlots[i]));
        }

        yield return new WaitForSeconds(spinDuration);

        for (int i = 0; i < 5; i++)
        {
            StopCoroutine(playerSpin[i]);
            StopCoroutine(enemySpin[i]);

            playerSlots[i].text = Random.Range(1, 10).ToString();
            enemySlots[i].text = Random.Range(1, 10).ToString();

            PlaySlotSound();

            StartCoroutine(PopSlot(playerSlots[i]));
            StartCoroutine(PopSlot(enemySlots[i]));

            yield return new WaitForSeconds(slotStopInterval);
        }

        HighlightSameNumbers(playerSlots);
        HighlightSameNumbers(enemySlots);

        int playerDamage = CalculateDamage(playerSlots);
        int enemyDamage = CalculateDamage(enemySlots);

        playerDamage =
            ApplyDamageMultiplier(
                playerDamage,
                playerDamageMultiplier
            );

        enemyDamage =
            ApplyDamageMultiplier(
                enemyDamage,
                enemyDamageMultiplier
            );

        playerDamageText.text = "내 공격: " + playerDamage;
        enemyDamageText.text = "상대 공격: " + enemyDamage;

        yield return new WaitForSeconds(0.5f);

        ResolveBattle(playerDamage, enemyDamage);

        yield return new WaitForSeconds(0.5f);

        turn++;

        // 턴 종료 → 아이템 하나 획득
        GetItem();

        UpdateUI();

        if (playerHP <= 0 || enemyHP <= 0)
        {
            EndBattle();
            yield break;
        }

        battleBusy = false;
        SetButtons(true);

        if (rouletteText != null)
            rouletteText.text = "룰렛 돌리기";
    }

    IEnumerator SpinSlot(TMP_Text slot)
    {
        while (true)
        {
            slot.text = Random.Range(1, 10).ToString();
            slot.color = normalSlotColor;

            yield return new WaitForSeconds(numberChangeSpeed);
        }
    }

    // =========================
    // 아이템
    // =========================

    void GetItem()
    {
        int currentCount = 0;

        for (int i = 0; i < 6; i++)
        {
            if (items[i] != "")
                currentCount++;
        }

        if (currentCount >= 6)
            return;

        // 랜덤으로 아이템 지급
        if (Random.value < 0.5f)
            items[currentCount] = "행운의 동전";
        else
            items[currentCount] = "연참";

        UpdateItemUI();
    }

    void UpdateItemUI()
    {
        if (itemTexts == null)
            return;

        for (int i = 0; i < 6; i++)
        {
            if (itemTexts[i] == null)
                continue;

            if (items[i] == "")
                itemTexts[i].text = "-";
            else
                itemTexts[i].text = items[i];
        }
    }

    public void UseItem(int slot)
    {
        if (battleEnded || battleBusy)
            return;

        if (slot < 0 || slot >= 6)
            return;

        if (items[slot] == "")
            return;

        string item = items[slot];

        // 아이템 제거
        items[slot] = "";

        // 뒤 아이템들을 앞으로 당김
        for (int i = slot; i < 5; i++)
        {
            items[i] = items[i + 1];
        }

        items[5] = "";

        UpdateItemUI();

        if (item == "행운의 동전")
        {
            UseLuckyCoin();
        }
        else if (item == "연참")
        {
            StartCoroutine(UseChainAttack());
        }
    }

    // =========================
    // 행운의 동전
    // =========================

    void UseLuckyCoin()
    {
        battleBusy = true;
        SetButtons(false);

        if (audioSource != null && itemUseSound != null)
            audioSource.PlayOneShot(itemUseSound);

        bool front = Random.value < 0.5f;

        if (front)
        {
            playerHP += 30;

            resultText.text =
                "행운의 동전: 앞면!\n내가 30 회복!";

            PlayHealEffect(playerHPText);
        }
        else
        {
            enemyHP += 30;

            resultText.text =
                "행운의 동전: 뒷면!\n상대가 30 회복!";

            PlayHealEffect(enemyHPText);
        }

        playerHP = Mathf.Max(0, playerHP);
        enemyHP = Mathf.Max(0, enemyHP);

        UpdateUI();

        StartCoroutine(ItemFinish());
    }

    // =========================
    // 연참
    // =========================

    IEnumerator UseChainAttack()
    {
        battleBusy = true;
        SetButtons(false);

        int playerDamage = 0;
        int enemyDamage = 0;

        string result = "";

        for (int i = 0; i < 3; i++)
        {
            bool front = Random.value < 0.5f;

            if (front)
            {
                result += "앞-";

                enemyHP -= 10;
                enemyDamage += 10;

                resultText.text =
                    "앞!\n상대에게 10 데미지!";

                PlayDamageEffect(enemyHPText);
            }
            else
            {
                result += "뒤-";

                playerHP -= 10;
                playerDamage += 10;

                resultText.text =
                    "뒤!\n나에게 10 데미지!";

                PlayDamageEffect(playerHPText);
            }

            if (audioSource != null && itemUseSound != null)
                audioSource.PlayOneShot(itemUseSound);

            UpdateUI();

            // 마지막 판정이 아니면 0.5초 대기
            if (i < 2)
                yield return new WaitForSeconds(0.5f);

            if (playerHP <= 0 || enemyHP <= 0)
                break;
        }

        // 결과 표시
        resultText.text =
            result +
            "\n\n상대 " + enemyDamage + " 데미지" +
            "\n나 " + playerDamage + " 데미지";

        yield return new WaitForSeconds(0.7f);

        if (playerHP <= 0 || enemyHP <= 0)
        {
            EndBattle();
            yield break;
        }

        battleBusy = false;
        SetButtons(true);
    }

    IEnumerator ItemFinish()
    {
        yield return new WaitForSeconds(0.7f);

        if (battleEnded)
            yield break;

        battleBusy = false;
        SetButtons(true);
    }

    // =========================
    // 슬롯 효과
    // =========================

    IEnumerator PopSlot(TMP_Text slot)
    {
        RectTransform rect = slot.GetComponent<RectTransform>();

        if (rect == null)
            yield break;

        Vector3 originalScale = rect.localScale;
        Vector3 bigScale = originalScale * popScale;

        float time = 0f;

        while (time < 1f)
        {
            time += Time.deltaTime * popSpeed;

            rect.localScale =
                Vector3.Lerp(
                    originalScale,
                    bigScale,
                    time
                );

            yield return null;
        }

        time = 0f;

        while (time < 1f)
        {
            time += Time.deltaTime * popSpeed;

            rect.localScale =
                Vector3.Lerp(
                    bigScale,
                    originalScale,
                    time
                );

            yield return null;
        }

        rect.localScale = originalScale;
    }

    void HighlightSameNumbers(TMP_Text[] slots)
    {
        int[] numbers = new int[5];

        for (int i = 0; i < 5; i++)
        {
            numbers[i] = int.Parse(slots[i].text);
            slots[i].color = normalSlotColor;
        }

        int[] count = new int[10];

        for (int i = 0; i < 5; i++)
            count[numbers[i]]++;

        bool[] soundPlayed = new bool[10];

        for (int i = 0; i < 5; i++)
        {
            int number = numbers[i];

            if (count[number] >= 2)
            {
                slots[i].color = sameNumberColor;

                StartCoroutine(
                    PopSlot(slots[i])
                );

                if (!soundPlayed[number])
                {
                    soundPlayed[number] = true;

                    if (audioSource != null &&
                        sameNumberSound != null)
                    {
                        audioSource.PlayOneShot(
                            sameNumberSound
                        );
                    }
                }
            }
        }
    }

    void ResetSlotColors()
    {
        if (playerSlots != null)
        {
            foreach (TMP_Text slot in playerSlots)
            {
                if (slot != null)
                    slot.color = normalSlotColor;
            }
        }

        if (enemySlots != null)
        {
            foreach (TMP_Text slot in enemySlots)
            {
                if (slot != null)
                    slot.color = normalSlotColor;
            }
        }
    }

    // =========================
    // 데미지 계산
    // =========================

    int ApplyDamageMultiplier(
        int damage,
        float multiplier
    )
    {
        return Mathf.RoundToInt(
            damage * multiplier
        );
    }

    int CalculateDamage(TMP_Text[] slots)
    {
        int[] numbers = new int[5];

        for (int i = 0; i < 5; i++)
            numbers[i] = int.Parse(slots[i].text);

        int[] count = new int[10];

        for (int i = 0; i < 5; i++)
            count[numbers[i]]++;

        if (count[6] == 5)
            return -666;

        if (count[6] == 3)
            return Random.Range(-66, 1);

        if (count[7] == 5)
            return 777;

        if (count[7] == 3)
            return Random.Range(10, 78);

        int maxCount = 0;

        for (int i = 1; i <= 9; i++)
            maxCount = Mathf.Max(
                maxCount,
                count[i]
            );

        if (maxCount == 5)
            return 5077;

        if (maxCount == 4)
            return Random.Range(30, 41);

        if (maxCount == 3)
            return Random.Range(20, 31);

        if (maxCount == 2)
            return Random.Range(10, 21);

        return Random.Range(-20, 11);
    }

    // =========================
    // 전투 결과
    // =========================

    void ResolveBattle(
        int playerDamage,
        int enemyDamage
    )
    {
        int result;

        if (playerDamage >= 0 &&
            enemyDamage >= 0)
        {
            result =
                playerDamage - enemyDamage;

            if (result > 0)
            {
                enemyHP -= result;

                resultText.text =
                    "최종값: " + result +
                    "\n상대에게 " + result +
                    " 데미지!";

                PlayDamageEffect(enemyHPText);
            }
            else if (result < 0)
            {
                int damage = Mathf.Abs(result);

                playerHP -= damage;

                resultText.text =
                    "최종값: " + damage +
                    "\n나에게 " + damage +
                    " 데미지!";

                PlayDamageEffect(playerHPText);
            }
            else
            {
                resultText.text =
                    "최종값: 0\n서로 상쇄!";
            }
        }
        else if (playerDamage >= 0 &&
                 enemyDamage < 0)
        {
            result =
                playerDamage + enemyDamage;

            if (result > 0)
            {
                enemyHP -= result;

                resultText.text =
                    "최종값: " + result +
                    "\n상대에게 " + result +
                    " 데미지!";

                PlayDamageEffect(enemyHPText);
            }
            else if (result < 0)
            {
                int heal = Mathf.Abs(result);

                enemyHP += heal;

                resultText.text =
                    "최종값: " + result +
                    "\n상대가 " + heal +
                    " 회복!";

                PlayHealEffect(enemyHPText);
            }
            else
            {
                resultText.text =
                    "최종값: 0\n서로 상쇄!";
            }
        }
        else if (playerDamage < 0 &&
                 enemyDamage >= 0)
        {
            result =
                playerDamage + enemyDamage;

            if (result > 0)
            {
                playerHP -= result;

                resultText.text =
                    "최종값: " + result +
                    "\n나에게 " + result +
                    " 데미지!";

                PlayDamageEffect(playerHPText);
            }
            else if (result < 0)
            {
                int heal = Mathf.Abs(result);

                playerHP += heal;

                resultText.text =
                    "최종값: " + result +
                    "\n내가 " + heal +
                    " 회복!";

                PlayHealEffect(playerHPText);
            }
            else
            {
                resultText.text =
                    "최종값: 0\n서로 상쇄!";
            }
        }
        else
        {
            result =
                playerDamage + enemyDamage;

            int heal = Mathf.Abs(result);

            playerHP += heal;

            resultText.text =
                "최종값: " + result +
                "\n내가 " + heal +
                " 회복!";

            PlayHealEffect(playerHPText);
        }

        playerHP = Mathf.Max(0, playerHP);
        enemyHP = Mathf.Max(0, enemyHP);
    }

    // =========================
    // HP 효과
    // =========================

    void PlayDamageEffect(TMP_Text hpText)
    {
        hpText.color = Color.red;

        if (audioSource != null &&
            damageSound != null)
        {
            audioSource.PlayOneShot(
                damageSound
            );
        }

        StartCoroutine(
            HPEffect(hpText)
        );
    }

    void PlayHealEffect(TMP_Text hpText)
    {
        hpText.color = Color.green;

        if (audioSource != null &&
            healSound != null)
        {
            audioSource.PlayOneShot(
                healSound
            );
        }

        StartCoroutine(
            HPEffect(hpText)
        );
    }

    IEnumerator HPEffect(TMP_Text hpText)
    {
        RectTransform rect =
            hpText.GetComponent<RectTransform>();

        if (rect == null)
            yield break;

        Vector3 originalScale =
            rect.localScale;

        Vector3 bigScale =
            originalScale * hpEffectScale;

        float time = 0f;

        while (time < 1f)
        {
            time +=
                Time.deltaTime *
                hpEffectSpeed;

            rect.localScale =
                Vector3.Lerp(
                    originalScale,
                    bigScale,
                    time
                );

            yield return null;
        }

        time = 0f;

        while (time < 1f)
        {
            time +=
                Time.deltaTime *
                hpEffectSpeed;

            rect.localScale =
                Vector3.Lerp(
                    bigScale,
                    originalScale,
                    time
                );

            yield return null;
        }

        rect.localScale =
            originalScale;

        hpText.color =
            Color.white;
    }

    // =========================
    // 버튼
    // =========================

    void SetButtons(bool interactable)
    {
        if (rouletteButton != null)
            rouletteButton.interactable =
                interactable;

        if (itemButtons != null)
        {
            foreach (Button button in itemButtons)
            {
                if (button != null)
                    button.interactable =
                        interactable;
            }
        }
    }

    // =========================
    // 사운드
    // =========================

    void PlaySlotSound()
    {
        if (audioSource != null &&
            slotStopSound != null)
        {
            audioSource.PlayOneShot(
                slotStopSound
            );
        }
    }

    // =========================
    // UI
    // =========================

    void UpdateUI()
    {
        playerHPText.text =
            "HP: " + playerHP;

        enemyHPText.text =
            "HP: " + enemyHP;

        turnText.text =
            "Turn: " + turn;
    }

    // =========================
    // 전투 종료
    // =========================

    void EndBattle()
    {
        if (battleEnded)
            return;

        battleEnded = true;

        SetButtons(false);

        TMP_Text rouletteText =
            rouletteButton.GetComponentInChildren<TMP_Text>();

        if (rouletteText != null)
            rouletteText.text = "종료";

        if (playerHP > enemyHP)
            resultText.text = "플레이어 승리!";
        else if (enemyHP > playerHP)
            resultText.text = "상대 승리!";
        else
            resultText.text = "무승부!";
    }
    public string GetItemDescription(int slot)
    {
        if (slot < 0 || slot >= 6)
            return "";

        if (items[slot] == "")
            return "";

        switch (items[slot])
        {
            case "행운의 동전":
                return "앞면 = 나 30 회복,\n뒷면 = 적 30 회복.";

            case "연참":
                return "3연 코인던지기. \n앞 = 적 10뎀,\n뒤 = 나 10뎀";

            default:
                return "";
        }
    }
    public void ShowItemDescription(string description)
    {
        if (descriptionUI != null)
            descriptionUI.Show(description);
    }
    public void HideItemDescription()
    {
        if (descriptionUI != null)
            descriptionUI.Hide();
    }
}