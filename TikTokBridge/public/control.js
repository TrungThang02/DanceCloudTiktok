const ui = {
    statusDot: document.getElementById('status-dot'),
    statusTitle: document.getElementById('status-title'),
    statusMessage: document.getElementById('status-message'),
    username: document.getElementById('username'),
    connect: document.getElementById('connect'),
    disconnect: document.getElementById('disconnect'),
    stopDemo: document.getElementById('stop-demo'),
    reset: document.getElementById('reset'),
    userIndex: document.getElementById('user-index'),
    joinMode: document.getElementById('join-mode'),
    giftAlwaysJoins: document.getElementById('gift-always-joins'),
    masterRules: document.getElementById('master-rules'),
    masterRuleTemplate: document.getElementById('master-rule-template'),
    addMasterRule: document.getElementById('add-master-rule'),
    saveMaster: document.getElementById('save-master'),
    masterMessage: document.getElementById('master-message'),
    recentGifts: document.getElementById('recent-gifts'),
    metrics: {
        events: document.getElementById('metric-events'),
        members: document.getElementById('metric-members'),
        chats: document.getElementById('metric-chats'),
        gifts: document.getElementById('metric-gifts'),
        diamonds: document.getElementById('metric-diamonds'),
        likes: document.getElementById('metric-likes')
    }
};

let socket;
let reconnectTimer;
let masterConfig = { joinMode: 'keyword_only', giftAlwaysJoins: true, rules: [] };
const recentGifts = new Map();

function send(message) {
    if (socket?.readyState === WebSocket.OPEN) {
        socket.send(JSON.stringify(message));
    }
}

function setStatus(data) {
    ui.statusDot.className = data.state || '';
    const names = {
        idle: 'Sẵn sàng',
        connecting: 'Đang kết nối',
        connected: 'TikTok đã kết nối',
        demo: 'Đang chạy demo',
        disconnected: 'Mất kết nối',
        ended: 'Live đã kết thúc',
        error: 'Có lỗi'
    };
    ui.statusTitle.textContent = names[data.state] || data.state;
    ui.statusMessage.textContent = data.message || '';
    ui.connect.disabled = data.state === 'connecting';
}

function setMetrics(data) {
    for (const [key, element] of Object.entries(ui.metrics)) {
        element.textContent = Number(data[key] || 0).toLocaleString('vi-VN');
    }
}

function makeRuleId() {
    return `rule-${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;
}

function emptyRule(overrides = {}) {
    return {
        id: makeRuleId(), enabled: true, source: 'gift', trigger: '', giftId: '', match: 'exact',
        action: 'dance', displayDiamonds: 0, durationMs: 3000, label: '', variant: '', fireworkBursts: 0, ...overrides
    };
}

function setMasterMessage(message, error = false) {
    ui.masterMessage.textContent = message || '';
    ui.masterMessage.classList.toggle('error', error);
}

function updateRuleSource(row) {
    const source = row.querySelector('[data-field="source"]').value;
    const giftId = row.querySelector('[data-field="giftId"]');
    giftId.disabled = source !== 'gift';
    giftId.placeholder = source === 'gift' ? 'Gift ID (nếu có)' : 'Không dùng cho bình luận';
}

function createRuleRow(rule) {
    const row = ui.masterRuleTemplate.content.firstElementChild.cloneNode(true);
    row.dataset.ruleId = rule.id || makeRuleId();
    for (const input of row.querySelectorAll('[data-field]')) {
        const field = input.dataset.field;
        if (field === 'durationSeconds') input.value = (Number(rule.durationMs) || 0) / 1000;
        else if (input.type === 'checkbox') input.checked = rule[field] !== false;
        else input.value = rule[field] ?? '';
    }
    row.querySelector('[data-field="source"]').addEventListener('change', () => updateRuleSource(row));
    row.querySelector('[data-command="delete"]').addEventListener('click', () => row.remove());
    row.querySelector('[data-command="test"]').addEventListener('click', () => {
        const config = readMasterFromUi();
        send({ type: 'master_save', master: config });
        send({ type: 'master_test', ruleId: row.dataset.ruleId, diamonds: 100 });
        setMasterMessage('Đang test luật trên Unity…');
    });
    updateRuleSource(row);
    return row;
}

function renderMaster(config) {
    masterConfig = config || masterConfig;
    ui.joinMode.value = masterConfig.joinMode || 'keyword_only';
    ui.giftAlwaysJoins.checked = masterConfig.giftAlwaysJoins !== false;
    ui.masterRules.replaceChildren(...(masterConfig.rules || []).map(createRuleRow));
}

function readMasterFromUi() {
    const rules = [...ui.masterRules.querySelectorAll('.master-rule-row')].map(row => ({
        id: row.dataset.ruleId,
        enabled: row.querySelector('[data-field="enabled"]').checked,
        source: row.querySelector('[data-field="source"]').value,
        trigger: row.querySelector('[data-field="trigger"]').value.trim(),
        giftId: row.querySelector('[data-field="giftId"]').value.trim(),
        match: row.querySelector('[data-field="match"]').value,
        action: row.querySelector('[data-field="action"]').value,
        displayDiamonds: Number(row.querySelector('[data-field="displayDiamonds"]').value) || 0,
        durationMs: Math.round((Number(row.querySelector('[data-field="durationSeconds"]').value) || 0) * 1000),
        label: row.querySelector('[data-field="label"]').value.trim(),
        variant: row.querySelector('[data-field="variant"]').value,
        fireworkBursts: Number(row.querySelector('[data-field="fireworkBursts"]').value) || 0
    }));
    return { joinMode: ui.joinMode.value, giftAlwaysJoins: ui.giftAlwaysJoins.checked, rules };
}

function observeGift(data) {
    const key = String(data.giftId || data.giftName || 'gift');
    recentGifts.set(key, data);
    while (recentGifts.size > 60) recentGifts.delete(recentGifts.keys().next().value);
    renderGiftCatalog();
}

function renderGiftCatalog() {
    ui.recentGifts.replaceChildren(...[...recentGifts.values()].reverse().map(gift => {
        const button = document.createElement('button');
        button.type = 'button';
        button.className = 'gift-catalog-item';
        if (gift.giftPictureUrl) {
            const image = document.createElement('img');
            image.src = gift.giftPictureUrl;
            image.alt = '';
            image.loading = 'lazy';
            button.append(image);
        }
        const text = document.createElement('span');
        text.textContent = `${gift.giftName || 'Gift'} · ID ${gift.giftId || '?'} · ${gift.diamondCount || 0}💎`;
        button.append(text);
        button.addEventListener('click', () => {
            ui.masterRules.append(createRuleRow(emptyRule({
                source: 'gift', trigger: gift.giftName || '', giftId: String(gift.giftId || ''),
                displayDiamonds: Number(gift.diamondCount) || 0, action: 'dance'
            })));
            setMasterMessage('Đã thêm gift vào cuối bảng. Chọn hành động rồi bấm Lưu & áp dụng.');
        });
        return button;
    }));
}

function connectSocket() {
    clearTimeout(reconnectTimer);
    const protocol = window.location.protocol === 'https:' ? 'wss:' : 'ws:';
    socket = new WebSocket(`${protocol}//${window.location.host}`);
    socket.addEventListener('open', () => {
        send({ type: 'register', role: 'control' });
    });
    socket.addEventListener('message', event => {
        let data;
        try {
            data = JSON.parse(event.data);
        } catch {
            return;
        }
        if (data.type === 'status' || data.type === 'error') setStatus(data);
        if (data.type === 'metrics') setMetrics(data);
        if (data.type === 'master_config') renderMaster(data.master);
        if (data.type === 'master_saved') setMasterMessage(data.message || 'Đã lưu Master.');
        if (data.type === 'gift_observed') observeGift(data);
        if (data.type === 'stage_theme') updateActiveThemeButton(data.themeIndex);
        if (data.type === 'gift_catalog') {
            recentGifts.clear();
            for (const gift of data.gifts || []) recentGifts.set(String(gift.giftId || gift.giftName || 'gift'), gift);
            renderGiftCatalog();
        }
        if (data.type === 'error') setMasterMessage(data.message || 'Có lỗi.', true);
    });
    socket.addEventListener('close', () => {
        setStatus({ state: 'disconnected', message: 'Đang kết nối lại máy chủ…' });
        reconnectTimer = setTimeout(connectSocket, 2000);
    });
    socket.addEventListener('error', () => socket.close());
}

function updateActiveThemeButton(activeIndex) {
    document.querySelectorAll('.theme-btn').forEach(btn => {
        const index = Number(btn.dataset.themeIndex);
        btn.classList.toggle('active', index === activeIndex);
    });
}

document.querySelectorAll('.theme-btn').forEach(button => {
    button.addEventListener('click', () => {
        const themeIndex = Number(button.dataset.themeIndex) || 0;
        const themeId = button.dataset.themeId || '';
        updateActiveThemeButton(themeIndex);
        send({
            type: 'stage_theme',
            themeIndex: themeIndex,
            themeId: themeId
        });
    });
});

ui.connect.addEventListener('click', () => {
    const username = ui.username.value.trim();
    if (username) send({ type: 'set_username', username });
});
ui.username.addEventListener('keydown', event => {
    if (event.key === 'Enter') ui.connect.click();
});
ui.disconnect.addEventListener('click', () => send({ type: 'disconnect_tiktok' }));
ui.stopDemo.addEventListener('click', () => send({ type: 'demo_stop' }));
ui.reset.addEventListener('click', () => send({ type: 'reset_game' }));
ui.addMasterRule.addEventListener('click', () => ui.masterRules.append(createRuleRow(emptyRule())));
ui.saveMaster.addEventListener('click', () => {
    send({ type: 'master_save', master: readMasterFromUi() });
    setMasterMessage('Đang lưu và áp dụng…');
});

document.querySelectorAll('[data-demo-count]').forEach(button => {
    button.addEventListener('click', () => {
        send({ type: 'demo_start', count: Number(button.dataset.demoCount) });
    });
});

document.querySelectorAll('[data-action]').forEach(button => {
    button.addEventListener('click', () => {
        send({
            type: 'demo_event',
            action: button.dataset.action,
            value: Number(button.dataset.value) || 1,
            giftName: button.dataset.giftName || '',
            userIndex: Number(ui.userIndex.value) || 1
        });
    });
});

connectSocket();
