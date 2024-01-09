from mlagents_envs.environment import UnityEnvironment
from mlagents_envs.envs.unity_gym_env import UnityToGymWrapper
from mlagents_envs.base_env import ActionTuple
import numpy as np
import ai
import gym_test.ai as ai2
import time
import matplotlib.pyplot as plt
import torch

def main():
	brain = ai.Dqn(input_size=9,output_size=5,gamma=0.99,lr=0.001)
	print("Brain initialised.\nPress the Play button in Unity editor")
	# This is a non-blocking call that only loads the environment.
	unity_env = UnityEnvironment(file_name=None)
	gym = UnityToGymWrapper(unity_env=unity_env, allow_multiple_obs=True)
	
	try:
		# Get number of actions from gym action space
		n_actions = gym.action_space.n
		# Get the number of state observations
		rewards, reward_episode, episode_duration = [], [], []
		step = 0
		state = gym.reset()
		n_observations = len(state)
		
		reset_gym(gym,brain)
		while(True):
			step += 1
			action = brain.select_actions(state)
			observation, reward, terminated, truncated = gym.step(action.item())
			observation = observation[0]
			#print_gym_status(action, observation, reward, terminated, truncated)
			state = brain.update(reward,observation,terminated)
			rewards.append(reward)
			if(terminated):
				reset_gym(gym,brain)
				reward_episode.append(reward)
				episode_duration.append(step)
				print(f"Reward: {reward} / Duration: {step}")
				step = 0

	finally:
		gym.close()
		brain.save()
		plot_reward_history(reward_episode,episode_duration)

def reset_gym(gym, brain):
	state = gym.reset()
	state = torch.tensor(state[0], dtype=torch.float32, device=brain.device).unsqueeze(0)
	brain.state = state


	'''
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
		'''
		
	

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


def plot_reward_history(rewards, durations):
	average_window = 15

	if len(rewards) >= average_window:
		mean_rewards, mean_durations = [],[]
		for i in range(0,average_window):
			mean_rewards.append(np.mean(rewards[0:i]))
			mean_durations.append(np.mean(durations[0:i]))
		for i in range(average_window,len(rewards)):
			mean_rewards.append(np.mean(rewards[i-average_window:i]))
			mean_durations.append(np.mean(durations[i-average_window:i]))

	fig, axs = plt.subplots(2,1, figsize=(16, 9))
	fig.suptitle('Rolling ball episodes info')
	axs[0].plot(rewards)
	axs[0].plot(mean_rewards)
	axs[0].set(ylabel=" Last reward", xlabel="Episode")
	axs[0].grid()
	axs[0].set

	axs[1].plot(durations)
	axs[1].plot(mean_durations)
	axs[1].set(ylabel="Duration (steps)", xlabel="Episode")
	axs[1].grid()
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

def print_gym_status(action, observation, reward, terminated, truncated):
	print("[Gym infos]")
	print(f"Action: {action}")
	print(f"Obs: {observation}")
	print(f"Reward: {reward}")
	print(f"Terminated: {terminated}")
	print(f"Truncated: {truncated}\n")
	
	
if __name__ == '__main__':
  main()