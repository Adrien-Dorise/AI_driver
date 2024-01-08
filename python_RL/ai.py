import numpy as np
import random
import os
import torch
import torch.nn as nn
import torch.nn.functional as F
import torch.optim as optim
import torch.autograd as autograd
from torch.autograd import Variable

from collections import namedtuple, deque
import math

class Network(nn.Module):
    
    def __init__(self, input_size, output_size):
        super(Network, self).__init__()
        self.input_size = input_size
        self.output_size = output_size
        self.fc_input = nn.Linear(input_size, 128)
        self.fc1 = nn.Linear(128,128)
        self.fc_output = nn.Linear(128, output_size)
    
    def forward(self, state):
        x = F.relu(self.fc_input(state))
        x = F.relu(self.fc1(x))
        q_values = self.fc_output(x)
        return q_values

# Implementing Experience Replay
class ReplayMemory(object):
    
    def __init__(self, capacity):
        self.Transition = namedtuple('Transition',
                        ('state', 'action', 'next_state', 'reward'))
        self.capacity = capacity
        self.memory = deque([], maxlen=capacity)
    
    def push(self, *args):
        """Save a transition"""
        self.memory.append(self.Transition(*args))
    
    def sample(self, batch_size):
        return random.sample(self.memory, batch_size)
    
    def __len__(self):
        return len(self.memory)
        

# Implementing Deep Q Learning
class Dqn():
    


    def __init__(self, input_size, output_size, batch_size=128, gamma=0.95, tau=0.005, lr=1e-3, eps_start=0.9, eps_end=0.05, eps_decay=10000):
        """ Implements the deep Q-learning algorithm

        Args:
            input_size (int): Number of observations given to the agent
            output_size (int): Number of possible actions performed by the agent
            batch_size (int, optional): Number of transitions sampled from the replay buffer. Defaults to 128.
            gamma (float, optional): Discount factor. Defaults to 0.95.
            tau (float, optional): Update rate of the target network. Defaults to 0.005.
            lr (_type_, optional): Learning rate of the optimiser. Defaults to 1e-3.
            eps_start (float, optional): Starting value of epsilon. Defaults to 0.9.
            eps_end (float, optional): Final value of epsilon. Defaults to 0.05.
            eps_decay (int, optional): Rate of the exponential decay of epsilon. Higher value means slower decay. Defaults to 1000.
        """
        self.device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
        self.gamma = gamma
        self.batch_size = batch_size
        self.tau = tau
        self.eps_start = eps_start
        self.eps_end = eps_end
        self.eps_decay = eps_decay
        self.output_size = output_size
        self.input_size = input_size
        self.policy_net = Network(input_size, output_size).to(self.device)
        self.target_net = Network(input_size, output_size).to(self.device)
        self.target_net.load_state_dict(self.policy_net.state_dict())
        self.optimizer = optim.AdamW(self.policy_net.parameters(), lr=lr, amsgrad=True)
        self.memory = ReplayMemory(100000)
        self.state = torch.Tensor(input_size).unsqueeze(0).to(self.device)
        self.last_action = 0
        self.steps_done = 0


    def init_state(self, observation):
        self.state = torch.tensor(observation, dtype=torch.float32, device=self.device).unsqueeze(0)
    
    def select_actions(self, state):
        print(state)
        if state is None:
            return torch.tensor([[0]])
        sample = random.random()
        eps_threshold = self.eps_end + (self.eps_start - self.eps_end) * \
            math.exp(-1. * self.steps_done / self.eps_decay)
        self.steps_done += 1
        if sample > eps_threshold:
            with torch.no_grad():
                # t.max(1) will return the largest column value of each row.
                # second column on max result is index of where max element was
                # found, so we pick action with the larger expected reward.
                action = self.policy_net(state).max(1).indices.view(1, 1)
        else:
            action =  torch.tensor([[random.randint(0,self.output_size-1)]], device=self.device, dtype=torch.long)
        
        self.last_action = action
        return action
    
    def learn(self, batch_size):
        transitions = self.memory.sample(batch_size)
        batch = self.memory.Transition(*zip(*transitions))
        
        non_final_mask = torch.tensor(tuple(map(lambda s: s is not None,
                                          batch.next_state)), device=self.device, dtype=torch.bool)
        non_final_next_states = torch.cat([s for s in batch.next_state
                                                    if s is not None])
        state_batch = torch.cat(batch.state)
        action_batch = torch.cat(batch.action)
        reward_batch = torch.cat(batch.reward)

        state_action_values = self.policy_net(state_batch).gather(1, action_batch)
        next_state_values = torch.zeros(batch_size, device=self.device)
        with torch.no_grad():
            next_state_values[non_final_mask] = self.target_net(non_final_next_states).max(1).values
        expected_state_action_values = (next_state_values * self.gamma) + reward_batch
        
        criterion = nn.SmoothL1Loss()
        loss = criterion(state_action_values, expected_state_action_values.unsqueeze(1))

        self.optimizer.zero_grad()
        loss.backward()
        # In-place gradient clipping
        torch.nn.utils.clip_grad_value_(self.policy_net.parameters(), 100)
        self.optimizer.step()

    
    def update(self, reward, observation, is_dead):
        #action = self.set_actions(self.state)

        reward = torch.tensor([reward], device=self.device)

        if is_dead:
            new_state = None
        else:
            new_state = torch.tensor(observation, dtype=torch.float32, device=self.device).unsqueeze(0)

        self.memory.push(self.state, self.last_action, new_state, reward)
        self.state = new_state
        
        if len(self.memory) > self.batch_size:
            self.learn(self.batch_size)

        self.target_net_state_dict = self.target_net.state_dict()
        self.policy_net_state_dict = self.policy_net.state_dict()
        for key in self.policy_net_state_dict:
            self.target_net_state_dict[key] = self.policy_net_state_dict[key]*self.tau + self.target_net_state_dict[key]*(1-self.tau)
        self.target_net.load_state_dict(self.target_net_state_dict)

        return new_state

    def save(self):
        torch.save({'state_dict': self.policy_net.state_dict(),
                    'optimizer' : self.optimizer.state_dict(),
                   }, 'last_brain.pth')
    
    def load(self):
        if os.path.isfile('last_brain.pth'):
            print("=> loading checkpoint... ")
            checkpoint = torch.load('last_brain.pth')
            self.policy_net.load_state_dict(checkpoint['state_dict'])
            self.optimizer.load_state_dict(checkpoint['optimizer'])
            print("done !")
        else:
            print("no checkpoint found...")


