using UnityEngine;

// SimpleNPC 类用于实现一个简单的非玩家角色（NPC）行为逻辑。
public class SimpleNPC1 : MonoBehaviour
{
    // 目标 Transform，NPC 会朝这个目标移动。
    public Transform[] target;
    public int PresentTarget;

    // 刚体组件，用于物理移动。
    private Rigidbody rb;

    // 移动速度。
    public float walkSpeed = 1;

    // 当前移动方向。
    private Vector3 dirToGo;

    // Awake 方法在游戏开始时调用，用于初始化。
    void Awake()
    {
        // 获取刚体组件。
        rb = GetComponent<Rigidbody>();
    }

    // Update 方法每帧调用一次，适合处理与时间相关的更新逻辑（当前未使用）。
    void Update()
    {
        // 当前没有在此处执行任何操作。
    }

    // FixedUpdate 方法在物理引擎更新时调用，适合处理物理相关操作。
    void FixedUpdate()
    {
        if ((target[PresentTarget].position - transform.position).magnitude <= 1)
        {
            int RandomTarget;
            do
            {
                RandomTarget = Random.Range(0, target.Length);
            } while (RandomTarget == PresentTarget);

            PresentTarget = RandomTarget;

        }
        // 计算从当前位置到目标位置的方向向量，并忽略 Y 轴（只在水平面上移动）。
        dirToGo = target[PresentTarget].position - transform.position;
        dirToGo.y = 0;

        // 旋转 NPC 的方向使其朝向目标。
        rb.rotation = Quaternion.LookRotation(dirToGo);

        // 沿着自身前方方向以固定速度移动。
        rb.MovePosition(transform.position+transform.forward * walkSpeed * Time.deltaTime);
    }

    // 设置随机行走速度的方法，用于增加 NPC 行为的多样性。
    public void SetRandomWalkSpeed()
    {
        // 随机生成一个介于 1 到 7 之间的速度值。
        walkSpeed = Random.Range(1f, 7f);
    }
}
