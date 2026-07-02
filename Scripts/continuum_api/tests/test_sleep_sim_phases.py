"""Tests for sleep wave phases."""

from continuum_api.sleep_sim import run_sleep_sim, _phase_at


def test_phase_electrical_sheep_at_start():
    assert _phase_at(0.05) == "ElectricalSheep"


def test_phase_rem_mid_night():
    assert _phase_at(0.80) == "REM"


def test_run_sleep_sim_samples():
    wave = run_sleep_sim({"dayCollapseSeed": 42}, 42, sample_count=128)
    assert len(wave["waveSamples"]) == 128
    assert wave["ioStats"]["entropyStart"] >= 0
