using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;

// DungeonEscapeEnvController 类用于管理迷宫逃脱环境的整体逻辑。
public class DungeonEscapeEnvController2 : MonoBehaviour
{
    // 玩家信息类，存储玩家代理及其初始状态和组件。
    [System.Serializable]
    public class PlayerInfo
    {
        public PushAgentEscape2 Agent; // 玩家代理对象
        [HideInInspector] public Vector3 StartingPos; // 初始位置
        [HideInInspector] public Quaternion StartingRot; // 初始旋转
        [HideInInspector] public Rigidbody Rb; // 刚体组件
        [HideInInspector] public Collider Col; // 碰撞器组件
    }

    // 敌人（龙）信息类，存储敌人代理及其初始状态和组件。
    [System.Serializable]
    public class DragonInfo
    {
        public SimpleNPC2 Agent; // 敌人代理对象
        [HideInInspector] public Vector3 StartingPos; // 初始位置
        [HideInInspector] public Quaternion StartingRot; // 初始旋转
        [HideInInspector] public Rigidbody Rb; // 刚体组件
        [HideInInspector] public Collider Col; // 碰撞器组件
        public Transform T; // 变换组件
        //public bool IsDead; // 是否死亡
    }

    // 最大训练步数，超过后重置场景。
    [Header("Max Environment Steps")] public int MaxEnvironmentSteps = 25000;
    private int m_ResetTimer; // 计时器

    // 场景区域边界，用于随机生成物体。
    [HideInInspector] public Bounds areaBounds;

    // 地面对象，用于获取边界和改变材质。
    public GameObject ground;

    Material m_GroundMaterial; // 存储地面初始材质
    Renderer m_GroundRenderer; // 地面渲染器

    // 玩家列表和敌人列表
    public List<PlayerInfo> AgentsList = new List<PlayerInfo>();
    public List<DragonInfo> DragonsList = new List<DragonInfo>();

    // 玩家字典，用于快速查找玩家信息
    private Dictionary<PushAgentEscape2, PlayerInfo> m_PlayerDict = new Dictionary<PushAgentEscape2, PlayerInfo>();

    // 是否使用随机旋转和位置
    public bool UseRandomAgentRotation = true;
    public bool UseRandomAgentPosition = true;

    PushBlockSettings m_PushBlockSettings; // 游戏设置

    private int m_NumberOfRemainingPlayers; // 剩余玩家数量
    public GameObject Key; // 钥匙对象
    public GameObject Tombstone; // 死亡后的墓碑对象
    public GameObject Switch;//按钮对象
    private SimpleMultiAgentGroup m_AgentGroup; // 多智能体组
    public int KeyTimer;
    public bool SwitchTriggered=false;
    void Start()
    {
        // 获取地面边界
        areaBounds = ground.GetComponent<Collider>().bounds;

        // 获取地面渲染器并缓存初始材质
        m_GroundRenderer = ground.GetComponent<Renderer>();
        m_GroundMaterial = m_GroundRenderer.material;

        // 获取游戏设置
        m_PushBlockSettings = FindObjectOfType<PushBlockSettings>();

        // 初始化剩余玩家数量
        m_NumberOfRemainingPlayers = AgentsList.Count;

        // 隐藏钥匙
        Key.SetActive(false);

        // 初始化多智能体组
        m_AgentGroup = new SimpleMultiAgentGroup();
        foreach (var item in AgentsList)
        {
            // 记录每个玩家的初始位置、旋转、刚体等信息
            item.StartingPos = item.Agent.transform.position;
            item.StartingRot = item.Agent.transform.rotation;
            item.Rb = item.Agent.GetComponent<Rigidbody>();
            item.Col = item.Agent.GetComponent<Collider>();
            m_AgentGroup.RegisterAgent(item.Agent);
        }

        // 记录敌人的初始信息
        foreach (var item in DragonsList)
        {
            item.StartingPos = item.Agent.transform.position;
            item.StartingRot = item.Agent.transform.rotation;
            item.T = item.Agent.transform;
            item.Col = item.Agent.GetComponent<Collider>();
        }

        // 重置场景
        ResetScene();
    }

    // 每帧调用一次，用于更新计时器并判断是否需要重置场景
    void FixedUpdate()
    {
        m_ResetTimer += 1;
        if (m_ResetTimer >= MaxEnvironmentSteps && MaxEnvironmentSteps > 0)
        {
            m_AgentGroup.GroupEpisodeInterrupted(); // 中断当前回合
            ResetScene(); // 重置场景
        }
        //
        // if (m_ResetTimer == KeyTimer)
        // {
        //     CreateKey();
        // }
    }

    // 当玩家碰到危险区域时调用
    public void TouchedHazard(PushAgentEscape2 agent)
    {
        m_NumberOfRemainingPlayers--; // 减少剩余玩家数量
        if (m_NumberOfRemainingPlayers == 0 || agent.IHaveAKey)
        {
            m_AgentGroup.EndGroupEpisode(); // 结束当前回合
            ResetScene(); // 重置场景
        }
        else
        {
            agent.gameObject.SetActive(false); // 隐藏玩家对象
        }
    }

    // 当玩家解锁门时调用
    public void UnlockDoor()
    {
        m_AgentGroup.AddGroupReward(1f); // 给所有玩家奖励
        StartCoroutine(GoalScoredSwapGroundMaterial(m_PushBlockSettings.goalScoredMaterial, 0.5f)); // 改变地面材质表示成功

        Debug.Log("Unlocked Door"); // 输出日志

        m_AgentGroup.EndGroupEpisode(); // 结束当前回合
        ResetScene(); // 重置场景
    }

    // 当玩家被敌人吃掉时调用
    public void KilledByBaddie(PushAgentEscape2 agent, Collision baddieCol)
    {
        //baddieCol.gameObject.SetActive(false); // 隐藏敌人对象
        m_NumberOfRemainingPlayers--; // 减少剩余玩家数量
        agent.gameObject.SetActive(false); // 隐藏玩家对象
        //Debug.Log($"{baddieCol.gameObject.name} ate {agent.transform.name}");

        // 显示墓碑
        Tombstone.transform.SetPositionAndRotation(agent.transform.position, agent.transform.rotation);
        Tombstone.SetActive(true);

        // 生成钥匙
        // Key.transform.SetPositionAndRotation(baddieCol.collider.transform.position,
        //     baddieCol.collider.transform.rotation);
        // Key.SetActive(true);
    }

    public void CreateKey()
    {
        if (!SwitchTriggered)
        {
            SwitchTriggered = true;
            Key.transform.SetPositionAndRotation(GetRandomSpawnPos(),GetRandomRot());
            Key.SetActive(true);
        }

    }
    public void CreateSwitch()
    {
        Switch.transform.SetPositionAndRotation(new Vector3(GetRandomSpawnPos().x,0,GetRandomSpawnPos().z),GetRandomRot());
        Switch.SetActive(true);
    }

    // 获取一个随机的生成点
    public Vector3 GetRandomSpawnPos()
    {
        var foundNewSpawnLocation = false;
        var randomSpawnPos = Vector3.zero;
        while (!foundNewSpawnLocation)
        {
            // 在边界范围内生成随机坐标
            var randomPosX = Random.Range(-areaBounds.extents.x * m_PushBlockSettings.spawnAreaMarginMultiplier,
                areaBounds.extents.x * m_PushBlockSettings.spawnAreaMarginMultiplier);

            var randomPosZ = Random.Range(-areaBounds.extents.z * m_PushBlockSettings.spawnAreaMarginMultiplier,
                areaBounds.extents.z * m_PushBlockSettings.spawnAreaMarginMultiplier);

            randomSpawnPos = ground.transform.position + new Vector3(randomPosX, 1f, randomPosZ);

            // 确保该位置没有其他物体阻挡
            if (!Physics.CheckBox(randomSpawnPos, new Vector3(2.5f, 0.01f, 2.5f)))
            {
                foundNewSpawnLocation = true;
            }
        }

        return randomSpawnPos;
    }

    // 更改地面材质一段时间后恢复
    IEnumerator GoalScoredSwapGroundMaterial(Material mat, float time)
    {
        m_GroundRenderer.material = mat;
        yield return new WaitForSeconds(time);
        m_GroundRenderer.material = m_GroundMaterial;
    }

    // 当敌人碰到障碍物时调用
    public void BaddieTouchedBlock()
    {
        m_AgentGroup.EndGroupEpisode(); // 结束当前回合
        StartCoroutine(GoalScoredSwapGroundMaterial(m_PushBlockSettings.failMaterial, 0.5f)); // 改变材质表示失败
        ResetScene(); // 重置场景
    }

    // 获取一个随机旋转角度
    Quaternion GetRandomRot()
    {
        return Quaternion.Euler(0, Random.Range(0.0f, 360.0f), 0);
    }

    // 重置整个场景
    void ResetScene()
    {
        m_ResetTimer = 0; // 重置计时器
        m_NumberOfRemainingPlayers = AgentsList.Count; // 重置玩家数量

        // 随机旋转平台
        var rotation = Random.Range(0, 4);
        var rotationAngle = rotation * 90f;
        transform.Rotate(new Vector3(0f, rotationAngle, 0f));

        // 重置所有玩家
        foreach (var item in AgentsList)
        {
            // 根据设置决定使用随机位置还是初始位置
            var pos = UseRandomAgentPosition ? GetRandomSpawnPos() : item.StartingPos;

            // 根据设置决定使用随机旋转还是初始旋转
            var rot = UseRandomAgentRotation ? GetRandomRot() : item.StartingRot;

            // 设置玩家代理的位置和旋转
            item.Agent.transform.SetPositionAndRotation(pos, rot);

            // 重置刚体的速度为零
            item.Rb.velocity = Vector3.zero;

            // 重置刚体的角速度为零（停止旋转）
            item.Rb.angularVelocity = Vector3.zero;

            // 隐藏钥匙图标（表示未持有钥匙）
            item.Agent.MyKey.SetActive(false);

            // 将“是否持有钥匙”状态设为 false
            item.Agent.IHaveAKey = false;

            // 激活玩家对象（用于新一轮训练）
            item.Agent.gameObject.SetActive(true);

            // 将该玩家代理重新注册到多智能体组中，以便参与训练
            m_AgentGroup.RegisterAgent(item.Agent);
        }

        // 隐藏钥匙
        Key.SetActive(false);

        // 隐藏墓碑
        Tombstone.SetActive(false);

        //随机按钮位置
        CreateSwitch();

        // 重置所有敌人
        // 遍历所有敌人（龙）信息，重置每个敌人的状态
        foreach (var item in DragonsList)
        {
            // 如果当前敌人对象为空（已被销毁），则直接返回，防止空引用异常
            if (!item.Agent)
                return;

            // 将敌人的位置和旋转重置为初始值
            item.Agent.transform.SetPositionAndRotation(item.StartingPos, item.StartingRot);

            // 设置敌人的随机行走速度，增加训练多样性
            item.Agent.SetRandomWalkSpeed();

            // 激活敌人对象，使其重新参与本轮训练
            item.Agent.gameObject.SetActive(true);
        }

    }
}
