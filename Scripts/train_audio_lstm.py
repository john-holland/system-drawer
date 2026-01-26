"""
Python training script for LSTM/RNN model.
Trains on environment + behavior tree embeddings → DSP generation spaces.

Usage:
    python train_audio_lstm.py --data_dir <path_to_training_data> --output_dir <path_to_models>
"""

import argparse
import json
import numpy as np
import os
import sys
from pathlib import Path

try:
    import torch
    import torch.nn as nn
    import torch.optim as optim
    from torch.utils.data import Dataset, DataLoader
    TORCH_AVAILABLE = True
except ImportError:
    TORCH_AVAILABLE = False
    print("Warning: PyTorch not available. Install with: pip install torch")

try:
    import tensorflow as tf
    from tensorflow import keras
    from tensorflow.keras import layers
    TF_AVAILABLE = True
except ImportError:
    TF_AVAILABLE = False
    print("Warning: TensorFlow not available. Install with: pip install tensorflow")

try:
    import onnx
    import onnxruntime as ort
    ONNX_AVAILABLE = True
except ImportError:
    ONNX_AVAILABLE = False
    print("Warning: ONNX not available. Install with: pip install onnx onnxruntime")


class AudioLSTMDataset(Dataset):
    """Dataset for audio LSTM training."""
    
    def __init__(self, data_dir):
        self.data_dir = Path(data_dir)
        self.samples = []
        self.load_data()
    
    def load_data(self):
        """Load training data from JSON files."""
        json_files = list(self.data_dir.glob("*.json"))
        
        for json_file in json_files:
            try:
                with open(json_file, 'r') as f:
                    data = json.load(f)
                    
                    # Extract features
                    env_features = np.array(data.get('environment_features', []))
                    behavior_tree_embedding = np.array(data.get('behavior_tree_embedding', []))
                    dsp_params = np.array(data.get('dsp_params', []))
                    sound_tags = np.array(data.get('sound_tags', []))
                    
                    # Combine input features
                    input_features = np.concatenate([
                        env_features,
                        behavior_tree_embedding,
                        sound_tags
                    ])
                    
                    self.samples.append({
                        'input': input_features,
                        'output': dsp_params
                    })
            except Exception as e:
                print(f"Error loading {json_file}: {e}")
    
    def __len__(self):
        return len(self.samples)
    
    def __getitem__(self, idx):
        sample = self.samples[idx]
        return {
            'input': torch.FloatTensor(sample['input']),
            'output': torch.FloatTensor(sample['output'])
        }


class AudioLSTMModel(nn.Module):
    """LSTM model for audio generation from environment + behavior tree."""
    
    def __init__(self, input_dim, hidden_dim=128, num_layers=2, output_dim=64):
        super(AudioLSTMModel, self).__init__()
        
        self.hidden_dim = hidden_dim
        self.num_layers = num_layers
        
        # LSTM layers
        self.lstm = nn.LSTM(
            input_dim,
            hidden_dim,
            num_layers,
            batch_first=True,
            dropout=0.2 if num_layers > 1 else 0
        )
        
        # Output layers
        self.fc1 = nn.Linear(hidden_dim, hidden_dim)
        self.relu = nn.ReLU()
        self.dropout = nn.Dropout(0.2)
        self.fc2 = nn.Linear(hidden_dim, output_dim)
    
    def forward(self, x):
        # x shape: (batch, seq_len, input_dim)
        lstm_out, _ = self.lstm(x)
        
        # Take last output
        last_output = lstm_out[:, -1, :]
        
        # Fully connected layers
        out = self.fc1(last_output)
        out = self.relu(out)
        out = self.dropout(out)
        out = self.fc2(out)
        
        return out


def train_pytorch_model(data_dir, output_dir, epochs=100, batch_size=32, learning_rate=0.001):
    """Train PyTorch LSTM model."""
    if not TORCH_AVAILABLE:
        print("PyTorch not available. Skipping PyTorch training.")
        return
    
    # Create dataset
    dataset = AudioLSTMDataset(data_dir)
    if len(dataset) == 0:
        print("No training data found!")
        return
    
    dataloader = DataLoader(dataset, batch_size=batch_size, shuffle=True)
    
    # Determine input/output dimensions from first sample
    sample = dataset[0]
    input_dim = sample['input'].shape[0]
    output_dim = sample['output'].shape[0]
    
    # Create model
    model = AudioLSTMModel(input_dim, output_dim=output_dim)
    criterion = nn.MSELoss()
    optimizer = optim.Adam(model.parameters(), lr=learning_rate)
    
    # Training loop
    print(f"Training PyTorch model for {epochs} epochs...")
    for epoch in range(epochs):
        total_loss = 0.0
        for batch in dataloader:
            inputs = batch['input'].unsqueeze(1)  # Add sequence dimension
            targets = batch['output']
            
            optimizer.zero_grad()
            outputs = model(inputs)
            loss = criterion(outputs, targets)
            loss.backward()
            optimizer.step()
            
            total_loss += loss.item()
        
        avg_loss = total_loss / len(dataloader)
        if (epoch + 1) % 10 == 0:
            print(f"Epoch {epoch + 1}/{epochs}, Loss: {avg_loss:.4f}")
    
    # Save model
    output_path = Path(output_dir)
    output_path.mkdir(parents=True, exist_ok=True)
    
    torch.save(model.state_dict(), output_path / "audio_lstm_pytorch.pth")
    print(f"Model saved to {output_path / 'audio_lstm_pytorch.pth'}")
    
    # Export to ONNX if available
    if ONNX_AVAILABLE:
        try:
            dummy_input = torch.randn(1, 1, input_dim)
            onnx_path = output_path / "audio_lstm.onnx"
            torch.onnx.export(
                model,
                dummy_input,
                str(onnx_path),
                input_names=['input'],
                output_names=['output'],
                dynamic_axes={'input': {0: 'batch_size'}, 'output': {0: 'batch_size'}}
            )
            print(f"ONNX model saved to {onnx_path}")
        except Exception as e:
            print(f"Error exporting to ONNX: {e}")


def train_tensorflow_model(data_dir, output_dir, epochs=100, batch_size=32):
    """Train TensorFlow/Keras LSTM model."""
    if not TF_AVAILABLE:
        print("TensorFlow not available. Skipping TensorFlow training.")
        return
    
    # Create dataset
    dataset = AudioLSTMDataset(data_dir)
    if len(dataset) == 0:
        print("No training data found!")
        return
    
    # Prepare data
    inputs = []
    outputs = []
    for i in range(len(dataset)):
        sample = dataset[i]
        inputs.append(sample['input'].numpy())
        outputs.append(sample['output'].numpy())
    
    X = np.array(inputs)
    y = np.array(outputs)
    
    # Reshape for LSTM (batch, timesteps, features)
    X = X.reshape(X.shape[0], 1, X.shape[1])
    
    input_dim = X.shape[2]
    output_dim = y.shape[1]
    
    # Create model
    model = keras.Sequential([
        layers.LSTM(128, return_sequences=False, input_shape=(1, input_dim)),
        layers.Dropout(0.2),
        layers.Dense(128, activation='relu'),
        layers.Dropout(0.2),
        layers.Dense(output_dim)
    ])
    
    model.compile(
        optimizer='adam',
        loss='mse',
        metrics=['mae']
    )
    
    # Train
    print(f"Training TensorFlow model for {epochs} epochs...")
    history = model.fit(
        X, y,
        epochs=epochs,
        batch_size=batch_size,
        validation_split=0.2,
        verbose=1
    )
    
    # Save model
    output_path = Path(output_dir)
    output_path.mkdir(parents=True, exist_ok=True)
    
    # Save as SavedModel
    model_path = output_path / "audio_lstm_tf"
    model.save(str(model_path))
    print(f"Model saved to {model_path}")
    
    # Save as TensorFlow Lite
    try:
        converter = tf.lite.TFLiteConverter.from_saved_model(str(model_path))
        tflite_model = converter.convert()
        tflite_path = output_path / "audio_lstm.tflite"
        with open(tflite_path, 'wb') as f:
            f.write(tflite_model)
        print(f"TensorFlow Lite model saved to {tflite_path}")
    except Exception as e:
        print(f"Error exporting to TensorFlow Lite: {e}")


def main():
    parser = argparse.ArgumentParser(description='Train LSTM/RNN model for audio generation')
    parser.add_argument('--data_dir', type=str, required=True,
                        help='Directory containing training data JSON files')
    parser.add_argument('--output_dir', type=str, default='./models',
                        help='Directory to save trained models')
    parser.add_argument('--epochs', type=int, default=100,
                        help='Number of training epochs')
    parser.add_argument('--batch_size', type=int, default=32,
                        help='Batch size for training')
    parser.add_argument('--learning_rate', type=float, default=0.001,
                        help='Learning rate')
    parser.add_argument('--framework', type=str, choices=['pytorch', 'tensorflow', 'both'],
                        default='both', help='Framework to use for training')
    
    args = parser.parse_args()
    
    if args.framework in ['pytorch', 'both']:
        train_pytorch_model(
            args.data_dir,
            args.output_dir,
            args.epochs,
            args.batch_size,
            args.learning_rate
        )
    
    if args.framework in ['tensorflow', 'both']:
        train_tensorflow_model(
            args.data_dir,
            args.output_dir,
            args.epochs,
            args.batch_size
        )
    
    print("Training complete!")


if __name__ == '__main__':
    main()
