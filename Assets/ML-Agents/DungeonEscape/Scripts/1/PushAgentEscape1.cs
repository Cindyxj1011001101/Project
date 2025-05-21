using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

// PushAgentEscape 是一个继承自 Agent 的类，用于在迷宫逃脱环境中训练智能体。
public class PushAgentEscape1 : Agent
{
    // 拾取到的钥匙对象，在拾取后激活显示。
    public GameObject MyKey;

    // 表示当前是否已经拾取了钥匙。
    public bool IHaveAKey;

    // 游戏设置，例如角色移动速度等参数。
    private PushBlockSettings m_PushBlockSettings;

    // 用于物理计算的角色刚体组件。
    private Rigidbody m_AgentRb;

    // 环境控制器，负责管理整个游戏状态（如解锁门、死亡事件等）。
    private DungeonEscapeEnvController1 m_GameController;

    // 初始化方法，在游戏开始时调用一次。
    public override void Initialize()
    {
        // 获取父物体上的环境控制器组件。
        m_GameController = GetComponentInParent<DungeonEscapeEnvController1>();

        // 获取自身的刚体组件。
        m_AgentRb = GetComponent<Rigidbody>();

        // 查找游戏设置对象。
        m_PushBlockSettings = FindObjectOfType<PushBlockSettings>();

        // 初始状态下隐藏钥匙图标。
        MyKey.SetActive(false);

        // 初始化为未持有钥匙状态。
        IHaveAKey = false;
    }

    // 每次训练回合开始时调用，重置钥匙状态。
    public override void OnEpisodeBegin()
    {
        MyKey.SetActive(false);
        IHaveAKey = false;
    }

    // 收集观察数据，供神经网络模型决策使用。
    public override void CollectObservations(VectorSensor sensor)
    {
        // 添加“是否持有钥匙”作为观察信息。
        sensor.AddObservation(IHaveAKey);
    }

    // 根据动作执行移动或旋转。
    public void MoveAgent(ActionSegment<int> act)
    {
        // 初始化移动和旋转方向。
        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;

        // 获取当前动作编号。
        var action = act[0];

        // 根据动作编号决定移动或旋转方向。
        switch (action)
        {
            case 1:
                dirToGo = transform.forward * 1f; // 向前移动
                break;
            case 2:
                dirToGo = transform.forward * -1f; // 向后移动
                break;
            case 3:
                rotateDir = transform.up * 1f; // 向右旋转
                break;
            case 4:
                rotateDir = transform.up * -1f; // 向左旋转
                break;
            case 5:
                dirToGo = transform.right * -0.75f; // 向左横移
                break;
            case 6:
                dirToGo = transform.right * 0.75f; // 向右横移
                break;
        }

        // 执行旋转。
        transform.Rotate(rotateDir, Time.fixedDeltaTime * 200f);

        // 施加力使角色移动。
        m_AgentRb.AddForce(dirToGo * m_PushBlockSettings.agentRunSpeed,
            ForceMode.VelocityChange);
    }

    // 每次引擎更新时调用，接收并执行动作。
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // 调用 MoveAgent 方法执行动作。
        MoveAgent(actionBuffers.DiscreteActions);
    }

    // 当发生碰撞时调用。
    void OnCollisionEnter(Collision col)
    {
        // 如果碰到锁且持有钥匙，则解锁。
        if (col.transform.CompareTag("lock"))
        {
            if (IHaveAKey)
            {
                MyKey.SetActive(false); // 隐藏钥匙图标
                IHaveAKey = false; // 设置为未持有钥匙
                m_GameController.UnlockDoor(); // 触发解锁逻辑
            }
        }

        // 如果碰到敌人（dragon），触发死亡逻辑。
        if (col.transform.CompareTag("dragon"))
        {
            m_GameController.KilledByBaddie(this, col); // 死亡回调
            MyKey.SetActive(false); // 丢弃钥匙
            IHaveAKey = false; // 设置为未持有钥匙
        }

        // 如果碰到传送门（portal），触发危险事件。
        if (col.transform.CompareTag("portal"))
        {
            m_GameController.TouchedHazard(this); // 触发危险事件
        }
    }

    // 当进入触发器区域时调用。
    void OnTriggerEnter(Collider col)
    {
        // 如果是钥匙，并且属于同一层级平台，可以拾取。
        if (col.transform.CompareTag("key") && col.transform.parent == transform.parent && gameObject.activeInHierarchy)
        {
            Debug.Log("Picked up key"); // 输出日志

            MyKey.SetActive(true); // 显示钥匙图标
            IHaveAKey = true; // 设置为已持有钥匙

            col.gameObject.SetActive(false); // 隐藏原钥匙对象
        }
    }

    // 提供一个手动控制策略（人类玩家测试用）。
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;

        // 键盘控制映射：
        if (Input.GetKey(KeyCode.D))
        {
            discreteActionsOut[0] = 3; // D键：向右旋转
        }
        else if (Input.GetKey(KeyCode.W))
        {
            discreteActionsOut[0] = 1; // W键：向前移动
        }
        else if (Input.GetKey(KeyCode.A))
        {
            discreteActionsOut[0] = 4; // A键：向左旋转
        }
        else if (Input.GetKey(KeyCode.S))
        {
            discreteActionsOut[0] = 2; // S键：向后移动
        }
    }
}
