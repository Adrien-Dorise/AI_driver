
from mlagents_envs.environment import UnityEnvironment
from mlagents_envs.base_env import ActionTuple
import numpy as np
import ai
import time
import matplotlib.pyplot as plt

def main():
	brain = ai.Dqn(8,2,0.9)
	print("Brain initialised.\nPress the Play button in Unity editor")
	# This is a non-blocking call that only loads the environment.
	env = UnityEnvironment(file_name=None)
	# Start interacting with the environment.
	try:
		env.reset()
		save_steps = 0
		reward_save = []
		behavior_names = env.behavior_specs.keys()
		for name in behavior_names:
			print(name)
			agent_name = name
		while True:
			if(save_steps >= 500):
				brain.save()
				save_steps = 0
			step = env.get_steps(name)
			#print_step_status(step[0], step[1])
			if(len(step[1].agent_id>0)):
				env.reset()
			obs = get_observations(step)
			reward = get_reward(step)
			reward_save.append(reward)
			act = brain.update(reward,obs)
			actions = ActionTuple(discrete=None, continuous=np.array([act], dtype=np.float32))
			env.set_actions(agent_name, actions)
			env.step()
			save_steps +=1
	finally:
		env.close()
		plot_reward_history(reward_save)

def get_observations(step):
	if(len(step[0].agent_id > 0)):
		return step[0].obs[0]
	else:
		return step[1].obs[0]

def get_reward(step):
	if(len(step[0].agent_id > 0)):
		return step[0].reward[0]
	else:
		return step[1].reward[0]

def plot_reward_history(rewards):
	plt.plot(rewards)
	plt.grid()
	plt.title("Rolling ball rewards")
	plt.xlabel("Steps")
	plt.ylabel("Reward")
	plt.savefig("rolling_ball_reward.png")
	plt.show()


def print_step_status(decision_step, terminal_step):
	print("[Decision step]")
	print("Obs: ", decision_step.obs)
	print(f"Reward: {decision_step.reward}")
	print(f"Agent ID: {decision_step.agent_id}\n")

	print("\n[Terminal step]")
	print("Obs: ", terminal_step.obs)
	print(f"Reward: {terminal_step.reward}")
	print(f"Agent ID: {terminal_step.agent_id}")
	print(f"Is dead: {terminal_step.interrupted}\n")
	
	
if __name__ == '__main__':
  main()