using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class RobotController : MonoBehaviour
{
    //大模型回复: {"actions":["MOVE_FORWARD","MOVE_FORWARD","MOVE_FORWARD","MOVE_FORWARD","TURN_RIGHT","MOVE_FORWARD","PICK_UP"]}
    public GameObject youtong;
    private GameObject obj;
    // 这个函数将在动画中被调用
    public void MyEventFunction()
    {
        Debug.Log("动画帧事件触发了！");
        // 你可以在这里放：播放音效、生成特效、开启受击判定等代码
        obj.transform.SetParent(lefthandTransform);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;
    }

    public float moveStep = 1.0f; // 假设每格1米
    public float moveDuration = 0.5f;
    private bool isBusy = false;
    private Animator ani;
    // 搬起东西时的挂载点（手）
    public Transform lefthandTransform;
    public Transform righthandTransform;

    private void Start()
    {
        ani = GetComponent<Animator>();
        isBusy = false;
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            string[] q = { "MOVE_FORWARD", "MOVE_FORWARD", "MOVE_FORWARD", "MOVE_FORWARD", "TURN_RIGHT", "MOVE_FORWARD", "PICK_UP" };
            ExecuteActions(q, youtong);
        }
    }
    public void ExecuteActions(string[] actions, GameObject targetCrate)
    {
        StartCoroutine(ProcessActionQueue(actions, targetCrate));
        obj = targetCrate;
    }

    IEnumerator ProcessActionQueue(string[] actions, GameObject targetCrate)
    {
        isBusy = true;
        foreach (string action in actions)
        {
            Debug.Log("执行动作: " + action);
            yield return StartCoroutine(PerformOneAction(action, targetCrate));
        }
        isBusy = false;
    }

    IEnumerator PerformOneAction(string action, GameObject crate)
    {
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        float elapsed = 0;

        // 忽略动画，直接用平滑移动模拟
        if (action == "MOVE_FORWARD")
        {
            ani.SetBool("w",true);
            Vector3 targetPos = transform.position + transform.forward * moveStep;
            while (elapsed < moveDuration)
            {
                transform.position = Vector3.Lerp(startPos, targetPos, elapsed / moveDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            transform.position = targetPos; // 修正误差
        }
        else if (action == "TURN_LEFT")
        {
            ani.SetBool("left", true);
            Quaternion targetRot = transform.rotation * Quaternion.Euler(0, -90, 0);
            while (elapsed < moveDuration)
            {
                transform.rotation = Quaternion.Slerp(startRot, targetRot, elapsed / moveDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            transform.rotation = targetRot;
            ani.SetBool("left", false);
        }
        else if (action == "TURN_RIGHT")
        {
            ani.SetBool("right", true);
            Quaternion targetRot = transform.rotation * Quaternion.Euler(0, 90, 0);
            while (elapsed < moveDuration)
            {
                transform.rotation = Quaternion.Slerp(startRot, targetRot, elapsed / moveDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            transform.rotation = targetRot;
            ani.SetBool("right", false);
        }
        else if (action == "PICK_UP")
        {
            ani.SetBool("get", true);
            // 搬起逻辑：把箱子变成手的子物体，归零坐标
            if (crate != null)
            {
                // 关闭红色描边 (恢复默认颜色，这里假设你有个方法)
                Renderer crateRend = crate.GetComponent<Renderer>();
                if (crateRend != null)
                {
                    crateRend.material.SetFloat("_IsSelected", 0.0f);
                }
            }
            yield return new WaitForSeconds(3.0f);
            float a = 0;
            Vector3 left_right_middle=(lefthandTransform.position+righthandTransform.position)/2;
            Vector3 offse=crate.transform.position-left_right_middle;
            while (a < 3)
            {
                a+= Time.deltaTime;

                crate.transform.position = (lefthandTransform.position + righthandTransform.position) / 2 + offse;
                yield return null;
            }
            //crate.transform.SetParent(lefthandTransform);
            //crate.transform.localPosition = Vector3.zero;
            //crate.transform.localRotation = Quaternion.identity;
        }
    }
}