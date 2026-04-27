using UnityEngine;
using UnityEngine.UI;

public class GazeManager : MonoBehaviour
{
    public float activationTime = 3.0f;
    public Image crosshair; // 十字准星（可选：变色提示）
    public NetworkManager networkManager;
    public RobotController robotController;

    private float timer = 0f;
    private GameObject currentGazedObject;
    private GameObject lastGazedObject;
    private bool isLocked = false; // 锁定注视功能

    // 旋转摄像机相关
    public Transform cameraPivot; // 最好摄像机有个父节点作为旋转中心
    public float rotateSpeed = 100f;

    void Update()
    {
        HandleCameraRotation();

        if (isLocked) return; // 如果已经触发，就不再检测

        Ray ray =transform.GetComponent<Camera>().ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider.CompareTag("Crate")) // 确保箱子Tag是 "Crate"
            {
                GameObject hitObj = hit.collider.gameObject;

                if (currentGazedObject != hitObj)
                {
                    ResetGaze(); // 换了个箱子，重置
                    currentGazedObject = hitObj;
                }

                timer += Time.deltaTime;

                // 可选：UI 反馈，比如准星根据时间变红
                if (crosshair) crosshair.color = Color.Lerp(Color.white, Color.red, timer / activationTime);

                if (timer >= activationTime)
                {
                    TriggerAction(hitObj);
                }
            }
            else
            {
                ResetGaze();
            }
        }
        else
        {
            ResetGaze();
        }
    }

    void HandleCameraRotation()
    {
        // 按住鼠标右键或根据需求旋转
        if (Input.GetMouseButton(1))
        {
            float h = Input.GetAxis("Mouse X");
            cameraPivot.RotateAround(cameraPivot.transform.position,Vector3.up, h * rotateSpeed * Time.deltaTime);
            float v = Input.GetAxis("Mouse Y");
            transform.RotateAround(cameraPivot.transform.position, transform.right, -v * rotateSpeed * Time.deltaTime);
        }
    }

    void ResetGaze()
    {
        timer = 0;
        if (crosshair) crosshair.color = Color.white;

        // 恢复上一个物体的颜色
        if (lastGazedObject != null)
        {
            Renderer rend = lastGazedObject.GetComponent<Renderer>();
            if (rend != null)
            {
                // 关闭描边：将 _IsSelected 设为 0
                rend.material.SetFloat("_IsSelected", 0.0f);
            }
            lastGazedObject = null;
        }
        currentGazedObject = null;
    }

    void TriggerAction(GameObject targetCrate)
    {
        Debug.Log("注视成功！锁定系统，发送请求...");
        isLocked = true;
        lastGazedObject = targetCrate;

        // 1. 变红
        Renderer rend = targetCrate.GetComponent<Renderer>();
        if (rend != null)
        {
            // 开启描边：将 _IsSelected 设为 1
            rend.material.SetFloat("_IsSelected", 1.0f);

            // 确保颜色是红色 (防止被之前的操作改过)
            rend.material.SetColor("_RimColor", Color.red);
        }

        // 2. 发送请求给 Server
        StartCoroutine(networkManager.SendRequest(
            robotController.transform.position,
            robotController.transform.forward,
            targetCrate.transform.position,
            (actions) => {
                // 3. 收到回复，开始执行
                robotController.ExecuteActions(actions, targetCrate);

                // 注意：这里没有解锁 isLocked，因为你要求是“搬起”
                // 如果需要重置，可以在 ExecuteActions 完成后通过回调重置 isLocked = false
            }
        ));
    }
}