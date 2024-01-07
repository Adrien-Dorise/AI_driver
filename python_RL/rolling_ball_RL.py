
from mlagents_envs.environment import UnityEnvironment
from mlagents_envs.base_env import ActionTuple
import numpy as np
import ai
import time
import matplotlib.pyplot as plt
import torch

def main():
	brain = ai.Dqn(input_size=8,output_size=5,gamma=0.99,lr=0.001)
	print("Brain initialised.\nPress the Play button in Unity editor")
	# This is a non-blocking call that only loads the environment.
	env = UnityEnvironment(file_name=None)
	# Start interacting with the environment.
	try:
		env.reset()
		reward_episode_agent0 = []
		behavior_names = env.behavior_specs.keys()
		for name in behavior_names:
			agent_name = name
		agent_number = len(env.get_steps(name)[0].agent_id)
		rewards = []
		for i in range(agent_number):
			rewards.append([])
				
		# Initialise model state 
		obs, reward, step = get_agent_infos(env, name, 0)
		brain.init_state(obs)
		state = brain.state
		while True:
			for agent_ID in range(agent_number):
				act = brain.select_actions(state).reshape(1,-1)
				actions = ActionTuple(discrete=np.array(act.cpu(), dtype=np.int32), continuous=None)
				env.set_action_for_agent(agent_name, agent_ID, actions)
				env.step()

				obs, rew, step = get_agent_infos(env, name, agent_ID)
				rewards[agent_ID].append(rew)
				dead = is_dead(step[1], agent_ID)

				state = brain.update(rew, obs, dead)
				
				if(dead):
					if(agent_ID == 0):
						reward_episode_agent0.append(sum(rewards[0])/(len(rewards[0])+1.))
						print(f"Episode reward: {reward_episode_agent0[-1]}")
					#env.reset()
					rewards[agent_ID] = []
			
	finally:
		env.close()
		brain.save()
		plot_reward_history(reward_episode_agent0)

def get_agent_infos(env, behaviour_name, agent_id):
	step = env.get_steps(behaviour_name)
	obs = get_observations_int(step, agent_id)
	reward = get_reward(step, agent_id)
	return obs, reward, step

def get_reward(step, agent_id):
	"""Get the reward for a specific agent 
	Step is divided into two parts: decision steps and terminal steps. 
	We need to search in both objects to find the corresponding reward.
	Note: As Agent is always in decision step, we seach first in terminalStep to ifnd latest reward.

	Args:
		step (step Object): _description_
		agent_id (int): Agent id

	Returns:
		float: Current reward for the specified agent
	"""
	for i in range(len(step[1].agent_id)):
		if(agent_id == i):
			return step[1].reward[i]
	for i in range(len(step[0].agent_id)):
		if(agent_id == i):
			return step[0].reward[i]

	return 0

def get_observations(step, agent_id):
	"""Get the observations of a specific agent 
	Step is divided into two parts: decision steps and terminal steps. 
	We need to search in both objects to find the corresponding reward

	Args:
		step (step Object): _description_
		agent_id (int): Agent id

	Returns:
		array[float]: Observations of the specified agent
	"""
	for i in range(len(step[0].agent_id)):
		if(agent_id == i):
			return step[0].obs[0][i]

	for i in range(len(step[1].agent_id)):
		if(agent_id == i):
			return step[1].obs[0][i]
	return 0

def get_observations_int(step, agent_id):
	"""Get the observations of a specific agent 
	Step is divided into two parts: decision steps and terminal steps. 
	We need to search in both objects to find the corresponding reward

	Args:
		step (step Object): _description_
		agent_id (int): Agent id

	Returns:
		array[int]: Observations of the specified agent
	"""
	for i in range(len(step[0].agent_id)):
		if(agent_id == i):
			return np.array(step[0].obs[0][i]*10,dtype=np.int32)

	for i in range(len(step[1].agent_id)):
		if(agent_id == i):
			return np.array([step[1].obs[0][i]]*10,dtype=np.int32)
	return 0

def is_dead(terminal_step, agent_id):
	"""Verifies if an agent has finished its episode

	Args:
		terminal_step (mlAgent object): Terminal step Unity object
		agent_id (int): _description_

	Returns:
		_type_: true if agent is dead, false otherwise
	"""
	for id in terminal_step.agent_id:
		if id == agent_id:
			return True
	else:
		return False


def plot_reward_history(rewards):
	plt.plot(rewards)
	plt.grid()
	plt.title("Rolling ball rewards")
	plt.xlabel("Episode")
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