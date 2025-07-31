from PyQt6.QtWidgets import (
    QApplication, QMainWindow, QWidget, QTabWidget, QLabel, QLineEdit,
    QPushButton, QRadioButton, QGroupBox, QVBoxLayout, QHBoxLayout,
    QGridLayout, QSizePolicy
)
from PyQt6.QtGui import QPixmap
from PyQt6.QtCore import Qt
import sys

class MainWindow(QMainWindow):
    def __init__(self):
        super().__init__()
        self.setWindowTitle("Configuration GUI")
        self.setMinimumSize(800, 600)

        # Create tab widget
        tabs = QTabWidget()
        tabs.addTab(self._create_camera_tab(), "Camera")
        tabs.addTab(self._create_sensor_tab(), "Sensors")
        tabs.addTab(self._create_control_tab(), "Connection")
        tabs.addTab(self._create_debug_tab(), "Value")

        self.setCentralWidget(tabs)

    def _create_camera_tab(self):
        camera_tab = QWidget()
        layout = QVBoxLayout()

        # Placeholder for camera feed
        self.camera_label = QLabel()
        self.camera_label.setAlignment(Qt.AlignmentFlag.AlignCenter)
        self.camera_label.setText("[Camera Feed Here]")
        self.camera_label.setSizePolicy(QSizePolicy.Policy.Expanding, QSizePolicy.Policy.Expanding)
        self.camera_label.setStyleSheet("background-color: #333; color: #fff; font-size: 18px;")

        layout.addWidget(self.camera_label)
        camera_tab.setLayout(layout)
        return camera_tab

    def _create_sensor_tab(self):
        sensor_tab = QWidget()
        layout = QGridLayout()
        layout.setSpacing(10)

        metrics = ["Temperature (C)", "Acceleration", "Pressure 1", "Pressure 2"]
        for row, metric in enumerate(metrics):
            lbl = QLabel(metric)
            val = QLineEdit()
            val.setReadOnly(True)
            layout.addWidget(lbl, row, 0)
            layout.addWidget(val, row, 1)

        sensor_tab.setLayout(layout)
        return sensor_tab

    def _create_control_tab(self):
        control_tab = QWidget()
        outer_layout = QVBoxLayout()

        # Port input and connect
        port_layout = QHBoxLayout()
        port_label = QLabel("Port:")
        self.port_input = QLineEdit()
        self.connect_btn = QPushButton("Connect")
        port_layout.addWidget(port_label)
        port_layout.addWidget(self.port_input)
        port_layout.addWidget(self.connect_btn)
        port_layout.addStretch()

        # Mode selector
        mode_group = QGroupBox("Mode")
        mode_layout = QVBoxLayout()
        self.auto_radio = QRadioButton("Autonomous")
        self.manual_radio = QRadioButton("Manual")
        mode_layout.addWidget(self.auto_radio)
        mode_layout.addWidget(self.manual_radio)
        mode_group.setLayout(mode_layout)

        outer_layout.addLayout(port_layout)
        outer_layout.addWidget(mode_group)
        outer_layout.addStretch()
        control_tab.setLayout(outer_layout)
        return control_tab

    def _create_debug_tab(self):
        debug_tab = QWidget()
        layout = QGridLayout()
        layout.setSpacing(15)

        # Pneumatics valves
        pneu_group = QGroupBox("Pneumatics")
        pneu_layout = QGridLayout()
        for i in range(5):
            pneu_layout.addWidget(QLabel(f"Valve {i+1}"), i, 0)
            on = QRadioButton("On")
            off = QRadioButton("Off")
            pneu_layout.addWidget(on, i, 1)
            pneu_layout.addWidget(off, i, 2)
        pneu_group.setLayout(pneu_layout)

        # Ball valves
        ball_group = QGroupBox("Ball Valves (Release valve)")
        ball_layout = QGridLayout()
        for i in range(4):
            ball_layout.addWidget(QLabel(f"Valve {i+1}"), i, 0)
            on = QRadioButton("On")
            off = QRadioButton("Off")
            pause = QRadioButton("Pause")
            ball_layout.addWidget(on, i, 1)
            ball_layout.addWidget(off, i, 2)
            ball_layout.addWidget(pause, i, 3)
        ball_group.setLayout(ball_layout)

        # Motors
        motors_group = QGroupBox("Motors")
        motors_layout = QGridLayout()
        for i in range(3):
            motors_layout.addWidget(QLabel(f"Motor {i+1}"), i, 0)
            fwd = QRadioButton("Forward")
            rev = QRadioButton("Reverse")
            off = QRadioButton("Off")
            speed = QLineEdit()
            speed.setPlaceholderText("Speed")
            ok = QPushButton("Ok")
            motors_layout.addWidget(fwd, i, 1)
            motors_layout.addWidget(rev, i, 2)
            motors_layout.addWidget(off, i, 3)
            motors_layout.addWidget(speed, i, 4)
            motors_layout.addWidget(ok, i, 5)
        motors_group.setLayout(motors_layout)

        # Assemble debug layout
        layout.addWidget(pneu_group, 0, 0)
        layout.addWidget(ball_group, 1, 0)
        layout.addWidget(motors_group, 0, 1, 2, 1)
        debug_tab.setLayout(layout)
        return debug_tab

if __name__ == '__main__':
    app = QApplication(sys.argv)
    window = MainWindow()
    window.show()
    sys.exit(app.exec())
