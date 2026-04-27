###定义
#这是unity灵眸系统对应的后端服务代码，使用Flask框架搭建一个简单的API服务器，接收机器人和箱子的位置与朝向信息，
# 调用DeepSeek大模型生成机器人移动和操作箱子的动作序列，并将结果返回给客户端。
#提示词设计上，系统提示词详细定义了坐标系、可用指令、路径规划策略和输出规则，确保大模型能够理解任务并生成符合要求的动作序列。
#用户提示词则结构化地提供了输入数据和明确的需求，进一步引导大模型生成正确的输出。整体设计旨在保证生成的动作序列既合理又符合预期的格式要求。
from flask import Flask, request, jsonify
from openai import OpenAI
import os
import json

app = Flask(__name__)

# 配置 DeepSeek 客户端
client = OpenAI(
    api_key=os.environ.get('DEEPSEEK_API_KEY'),
    base_url="https://api.deepseek.com"
)

@app.route('/ask_action', methods=['POST'])
def ask_action():
    data = request.json
    print(f"收到请求: {data}")
    
    robot_pos = data['robot_pos']     # 例如 [0, 0]
    robot_face = data['robot_face']   # 例如 "North"
    crate_pos = data['crate_pos']     # 例如 [3, 2]

    system_prompt = """
    你是一个二维网格仓库机器人的路径规划系统。
    你的任务是输出一个JSON动作序列，控制机器人从[当前点]移动到[箱子的相邻点]，面向箱子，并执行搬起。

    ### 坐标系定义
    - **North (北)**: Y轴正方向 (+Y)
    - **South (南)**: Y轴负方向 (-Y)
    - **East (东)**: X轴正方向 (+X)
    - **West (西)**: X轴负方向 (-X)

    ### 可用指令
    1. "MOVE_FORWARD": 向当前朝向移动1格。
    2. "TURN_LEFT": 左转90度 (例: North -> West)。
    3. "TURN_RIGHT": 右转90度 (例: North -> East)。
    4. "PICK_UP": **必须是最后一个动作**。执行时机器人必须位于箱子相邻格(上下左右均可)，且**朝向**必须正对箱子。

    ### 路径规划策略 (请严格遵循此逻辑)
    为了保证路径最短且准确，请按以下步骤思考：
    1. **确定目标位**：选择箱子四周(上下左右)中距离机器人最近的一个格子作为“站立点”。
    2. **移动逻辑**：
    - 优先调整朝向并移动，消除 X 轴的坐标差。
    - 然后调整朝向并移动，消除 Y 轴的坐标差。
    - 到达“站立点”后，调整朝向使其正对箱子。
    3. **结束**：输出 "PICK_UP"。

    ### 示例 (Few-Shot)
    示例 1 (需要先走Y轴，再走X轴):
    输入: Robot:(0,0) Face:North -> Crate:(2,2)
    逻辑: 目标站立点选(2,1)。先走到(0,1)，再转东走到(2,1)，最后转北面对(2,2)。
    输出: ["MOVE_FORWARD", "TURN_RIGHT", "MOVE_FORWARD", "MOVE_FORWARD", "TURN_LEFT", "PICK_UP"]

    示例 2 (简单的直线):
    输入: Robot:(0,0) Face:East -> Crate:(3,0)
    逻辑: 目标站立点(2,0)。机器人已面向东，直接走两步到(2,0)，无需转向直接搬起。
    输出: ["MOVE_FORWARD", "MOVE_FORWARD", "PICK_UP"]

    ### 输出规则
    - 只输出纯 JSON 字符串数组。
    - 不要使用 Markdown 代码块 (```json)。
    - 不要包含任何解释性文字。
    """

    # 用户提示词也需要微调，使其更结构化
    user_prompt = f"""
    请规划路径：
    [Data]
    Robot_Pos: {robot_pos}
    Robot_Face: {robot_face}
    Target_Crate_Pos: {crate_pos}

    [Requirement]
    Generate the JSON action list.
    """

    # 2. 调用 DeepSeek
    try:
        response = client.chat.completions.create(
            model="deepseek-reasoner",       #deepseek-chat  deepseek-reasoner
            messages=[
                {"role": "system", "content": system_prompt},
                {"role": "user", "content": user_prompt},
            ],
            stream=False,
            temperature=0.0 # 降低随机性，保证逻辑严密
        )
        
        content = response.choices[0].message.content
        print(f"大模型原始回复: {content}")

        # 清洗数据，防止模型偶尔输出 ```json ... ```
        clean_content = content.replace("```json", "").replace("```", "").strip()
        actions = json.loads(clean_content)
        
        return jsonify({"actions": actions})

    except Exception as e:
        print(f"Error: {e}")
        return jsonify({"error": str(e)}), 500

if __name__ == '__main__':
    # 监听所有IP，端口5000
    app.run(host='0.0.0.0', port=5000)