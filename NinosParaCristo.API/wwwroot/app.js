const $ = (s) => document.querySelector(s);
const state = {
  token: localStorage.getItem("npc_token") || "",
  user: JSON.parse(localStorage.getItem("npc_user") || "null"),
  page: "inicio",
  hub: null,
  credentialsPending: false
};

const roles = {
  admin: "Administradora",
  clase: "MaestraClase",
  protocolo: "MaestraProtocolo",
  rotativa: "MaestraRotativa"
};
window.addEventListener("beforeunload", event=>{
  if(state.credentialsPending){event.preventDefault();event.returnValue="";}
});

function toast(msg){
  const t=$("#toast"); t.textContent=msg; t.classList.add("show");
  setTimeout(()=>t.classList.remove("show"),2600);
}
function esc(v){return String(v??"").replace(/[&<>"']/g,m=>({"&":"&amp;","<":"&lt;",">":"&gt;",'"':"&quot;","'":"&#039;"}[m]));}
function dateFmt(v){ if(!v) return ""; const [y,m,d]=v.substring(0,10).split("-"); return `${d}/${m}/${y}`; }
const meses=["Enero","Febrero","Marzo","Abril","Mayo","Junio","Julio","Agosto","Septiembre","Octubre","Noviembre","Diciembre"];
function trimestreActual(){ return Math.floor(new Date().getMonth()/3)+1; }
function rangoTrimestre(anio,trimestre){ const m=(trimestre-1)*3; const desde=`${anio}-${String(m+1).padStart(2,"0")}-01`; const ultimo=new Date(anio,m+3,0); const hasta=`${anio}-${String(m+3).padStart(2,"0")}-${String(ultimo.getDate()).padStart(2,"0")}`; return {desde,hasta,meses:[m+1,m+2,m+3]}; }
function proximoDomingo(){ const d=new Date(); const delta=(7-d.getDay())%7; d.setDate(d.getDate()+delta); return d; }
function monthYearLabel(m,y){ return `${meses[m-1]} ${y}`; }
async function api(path, options={}){
  const headers={"Content-Type":"application/json",...(options.headers||{})};
  if(state.token) headers.Authorization=`Bearer ${state.token}`;
  const res=await fetch(`/api${path}`,{...options,headers});
  if(res.status===401){ logout(); throw new Error("Sesión vencida."); }
  if(res.status===204) return null;
  const body=await res.json().catch(()=>null);
  if(!res.ok) throw new Error(body?.mensaje || body?.detail || body?.title || `Error ${res.status}`);
  return body;
}
function saveSession(data){
  state.token=data.token; state.user=data.usuario;
  localStorage.setItem("npc_token",state.token); localStorage.setItem("npc_user",JSON.stringify(state.user));
}
function logout(){
  closeMobileMenu();
  state.credentialsPending=false;
  $("#content").innerHTML="";
  if(state.hub){ state.hub.stop().catch(()=>{}); state.hub=null; }
  localStorage.removeItem("npc_token"); localStorage.removeItem("npc_user");
  state.token="";state.user=null;state.page="inicio";
  $("#appView").classList.add("hidden");$("#loginView").classList.remove("hidden");
}
function navItems(){
  const r=state.user?.rol;
  if(r===roles.admin) return [
    ["inicio","🏠  Inicio"],["ninos","👧  Niños"],["asistencia","📋  Asistencia"],["calendarios","📅  Calendarios de clase"],
    ["protocolo","🥪  Meriendas y protocolo"],["inventario","📦  Inventario"],["reportes","📊  Reportes"],
    ["importaciones","📥  Planificar e importar"],["comunicaciones","💬  Comunicación con padres"],["usuarios","👩‍🏫  Usuarios"],["notificaciones","🔔  Notificaciones"]
  ];
  if(r===roles.rotativa) return [["inicio","🏠 Inicio"],["aula","👧 Niños del aula"],["asistencia","✅ Pasar asistencia"],["calendarios","📅 Mis clases"],["protocolo","🥪 Mi protocolo"],["notificaciones","🔔 Notificaciones"]];
  if(r===roles.clase) return [["inicio","🏠  Inicio"],["aula","👧  Niños del aula"],["asistencia","✅  Pasar asistencia"],["calendarios","📅  Mi calendario"],["notificaciones","🔔  Notificaciones"]];
  return [["inicio","🏠  Inicio"],["protocolo","📅  Mi calendario"],["notificaciones","🔔  Notificaciones"]];
}
function buildNav(){
  $("#roleLabel").textContent=state.user.rol;
  $("#userLabel").textContent=state.user.nombreCompleto;
  const nav=$("#nav");nav.innerHTML="";
  for(const [id,label] of navItems()){
    const b=document.createElement("button");b.textContent=label;b.dataset.page=id;
    b.classList.toggle("active",id===state.page);
    b.onclick=()=>navigate(id);nav.appendChild(b);
  }
}
async function navigate(page){
  if(state.credentialsPending && !confirm("¿Ya guardaste las contraseñas iniciales? Al salir no podrás volver a verlas.")) return;
  state.credentialsPending=false;
  state.page=page;buildNav();closeMobileMenu();
  const labels=Object.fromEntries(navItems().map(x=>[x[0],x[1]]));$("#pageTitle").textContent=labels[page]||"Inicio";
  $("#content").innerHTML='<div class="card">Cargando...</div>';
  window.scrollTo({top:0,behavior:"instant"});
  try{ await pages[page](); }catch(e){ $("#content").innerHTML=`<div class="card"><b>Error:</b> ${esc(e.message)}</div>`; }
}
async function connectHub(){
  if(!window.signalR || !state.token) return;
  try{
    if(state.hub) await state.hub.stop();
    state.hub=new signalR.HubConnectionBuilder().withUrl("/hubs/updates",{accessTokenFactory:()=>state.token}).withAutomaticReconnect().build();
    state.hub.on("actualizacion",()=>{
      if(!state.token || state.credentialsPending || state.page==="importaciones") return;
      // Una actualización remota no debe borrar formularios ni el pase de lista en curso.
      if(document.querySelector("#content form")) { toast("Hay cambios disponibles. Actualiza después de guardar."); return; }
      navigate(state.page);
    });
    await state.hub.start();
  }catch(e){ console.warn("SignalR no disponible",e); }
}
function appStart(){
  $("#loginView").classList.add("hidden");$("#appView").classList.remove("hidden");
  buildNav();connectHub();
  const requested=location.hash.replace("#","");
  const valid=navItems().some(x=>x[0]===requested);
  navigate(valid?requested:"inicio");
  refreshPwaButtons();
}

$("#loginForm").addEventListener("submit",async e=>{
  e.preventDefault();
  try{
    const data=await api("/auth/login",{method:"POST",body:JSON.stringify({email:$("#loginEmail").value,password:$("#loginPassword").value})});
    saveSession(data);appStart();toast("Bienvenida");
  }catch(err){toast(err.message);}
});
$("#logoutBtn").onclick=logout;
$("#menuBtn").onclick=()=>openMobileMenu();
$("#refreshBtn").onclick=()=>navigate(state.page);

const pages={};

pages.inicio=async()=>{
  if(state.user.rol===roles.admin){
    const r=await api("/reportes/resumen");
    $("#content").innerHTML=`<div class="cards">
      <div class="card"><div class="muted">👧 Niños activos</div><div class="metric">${r.totalNinos}</div></div>
      <div class="card"><div class="muted">💚 Con alergias registradas</div><div class="metric">${r.ninosConAlergias}</div></div>
      <div class="card"><div class="muted">✅ Asistencia promedio · 90 días</div><div class="metric">${r.asistenciaPromedioUltimos90Dias}%</div></div>
      <div class="card"><div class="muted">📦 Materiales por reponer</div><div class="metric">${r.inventarioPorReponer}</div></div>
    </div><div class="card" style="margin-top:16px"><h3>💙 Panel administrativo</h3><p>Gestiona la información del ministerio desde un solo lugar. Los cambios quedan centralizados para que las maestras consulten la información actualizada desde sus dispositivos.</p></div>`;
  }else if((state.user.rol===roles.clase || state.user.rol===roles.rotativa)){
    const ahora=proximoDomingo(); const anio=ahora.getFullYear(); const mes=ahora.getMonth()+1;
    const [asigs,notifs]=await Promise.all([api(`/calendarios/clases?anio=${anio}&mes=${mes}`),api("/notificaciones/mias")]);
    const domingo=proximoDomingo();
    $("#content").innerHTML=`<div class="cards">
      <div class="card"><div class="muted">📚 Grupos para el próximo domingo</div><div class="metric">${asigs.length}</div></div>
      <div class="card"><div class="muted">🔔 Notificaciones pendientes</div><div class="metric">${notifs.filter(x=>!x.leida).length}</div></div>
    </div><div class="card" style="margin-top:16px"><h3>🌈 Próximo domingo · ${domingo.toLocaleDateString("es-CR")}</h3>${asigs.length?asigs.map(a=>`<div class="assignment-card"><b>${esc(a.grupo)}</b><span>${a.edadMinima}-${a.edadMaxima} años</span><p>En el mes de esta clase compartes el grupo con las maestras asignadas en el calendario.</p>${a.materialUrl?`<a href="${esc(a.materialUrl)}" target="_blank">📥 Abrir material</a>`:`<span class="muted">Material pendiente</span>`}</div>`).join(""):"<p>No tienes un grupo asignado para el próximo domingo.</p>"}</div>`;
  }else{
    const [p,notifs]=await Promise.all([api("/protocolo"),api("/notificaciones/mias")]);
    $("#content").innerHTML=`<div class="cards">
      <div class="card"><div class="muted">📅 Asignaciones próximas</div><div class="metric">${p.length}</div></div>
      <div class="card"><div class="muted">🔔 Notificaciones pendientes</div><div class="metric">${notifs.filter(x=>!x.leida).length}</div></div>
    </div><div class="card" style="margin-top:16px"><h3>🥪 Próxima asignación</h3>${p[0]?`<p><b>${dateFmt(p[0].fecha)}</b></p><p>Merienda: ${esc(p[0].merienda||"Por definir")}</p><p>Tareas: ${esc(p[0].tareas||"Por definir")}</p>`:"<p>No tienes asignaciones próximas.</p>"}</div>`;
  }
};

pages.ninos=async()=>{
  const ninos=await api("/ninos");
  $("#content").innerHTML=`<div class="card">
    <div class="section-title"><h3>Registrar niño</h3></div>
    <form id="ninoForm" class="form-grid">
      <label>Nombre completo<input id="nNombre" required maxlength="150"></label>
      <label>Fecha de nacimiento<input id="nFecha" type="date" required></label>
      <label>Alergias<input id="nAlergias" maxlength="500"></label>
      <label>Parentesco del encargado<input id="nParentesco" maxlength="80"></label>
      <label class="full">Comentarios<textarea id="nComentarios" maxlength="1000"></textarea></label>
      <label>Encargado principal<input id="nEncargado" maxlength="150"></label>
      <label>Teléfono<input id="nTelefono" maxlength="50"></label>
      <label>Email encargado<input id="nEmail" type="email"></label>
      <div class="full"><button>Guardar niño</button></div>
    </form>
  </div>
  <div class="card" style="margin-top:16px"><div class="section-title"><h3>Niños registrados</h3></div>
    <div class="table-wrap"><table><thead><tr><th>Nombre</th><th>Nacimiento</th><th>Alergias</th><th>Encargado</th><th>Estado</th><th></th></tr></thead>
    <tbody>${ninos.map(n=>`<tr><td>${esc(n.nombreCompleto)}</td><td>${dateFmt(n.fechaNacimiento)}</td><td>${esc(n.alergias||"—")}</td><td>${esc(n.encargadoPrincipal?.nombre||"—")}<br><small>${esc(n.encargadoPrincipal?.telefono||"")}</small></td><td><span class="badge ${n.activo?"":"danger"}">${n.activo?"Activo":"Inactivo"}</span></td><td class="actions"><button class="ghost btn-small" onclick="editarNino(${n.id})">Editar</button>${n.activo?`<button class="ghost danger btn-small" onclick="desactivarNino(${n.id})">Desactivar</button>`:""}</td></tr>`).join("")}</tbody></table></div>
  </div>`;
  $("#ninoForm").onsubmit=async e=>{
    e.preventDefault();
    try{
      await api("/ninos",{method:"POST",body:JSON.stringify({
        nombreCompleto:$("#nNombre").value,fechaNacimiento:$("#nFecha").value,alergias:$("#nAlergias").value||null,
        comentarios:$("#nComentarios").value||null,encargadoNombre:$("#nEncargado").value||null,
        encargadoTelefono:$("#nTelefono").value||null,encargadoEmail:$("#nEmail").value||null,encargadoParentesco:$("#nParentesco").value||null
      })});toast("Niño registrado");navigate("ninos");
    }catch(err){toast(err.message);}
  };
};
window.editarNino=async id=>{
  const n=await api(`/ninos/${id}`);
  const nombre=prompt("Nombre completo",n.nombreCompleto);if(nombre===null)return;
  const fecha=prompt("Fecha de nacimiento (AAAA-MM-DD)",n.fechaNacimiento);if(fecha===null)return;
  const alergias=prompt("Alergias",n.alergias||"");if(alergias===null)return;
  const comentarios=prompt("Comentarios",n.comentarios||"");if(comentarios===null)return;
  await api(`/ninos/${id}`,{method:"PUT",body:JSON.stringify({nombreCompleto:nombre,fechaNacimiento:fecha,alergias,comentarios,activo:n.activo})});
  toast("Actualizado");navigate("ninos");
};
window.desactivarNino=async id=>{if(confirm("¿Desactivar este niño?")){await api(`/ninos/${id}`,{method:"DELETE"});toast("Desactivado");navigate("ninos");}};

pages.aula=async()=>{
  const ninos=await api("/ninos/aula");
  $("#content").innerHTML=`<div class="card"><div class="section-title"><h3>Información necesaria para el aula</h3></div>
  <div class="notice">Por protección de datos, esta vista solo muestra el nombre y las alergias.</div>
  <div class="table-wrap"><table><thead><tr><th>Nombre</th><th>Grupo</th><th>Alergias</th></tr></thead><tbody>${ninos.map(n=>`<tr><td>${esc(n.nombreCompleto)}</td><td>${esc(n.grupo||"—")}</td><td>${esc(n.alergias||"Sin alergias registradas")}</td></tr>`).join("")}</tbody></table></div></div>`;
};

pages.asistencia=async()=>{
  const now=new Date();
  const today=`${now.getFullYear()}-${String(now.getMonth()+1).padStart(2,"0")}-${String(now.getDate()).padStart(2,"0")}`;
  $("#content").innerHTML=`<div class="card"><div class="toolbar"><label>Fecha<input id="aFecha" type="date" value="${today}"></label><button id="aCargar">Cargar lista</button></div><div id="aLista"></div></div>`;
  const load=async()=>{
    const fecha=$("#aFecha").value;const [rows,merienda]=await Promise.all([api(`/asistencias?fecha=${fecha}`),api(`/protocolo/merienda?fecha=${fecha}`)]);
    $("#aLista").innerHTML=`<div class="notice"><b>Merienda del día:</b> ${esc(merienda.merienda||"No definida")}</div>${rows.length?`<form id="aForm"><div class="table-wrap"><table><thead><tr><th>Niño</th><th>Grupo</th><th>Alergias</th><th>Presente</th><th>Observación</th></tr></thead><tbody>
    ${rows.map(r=>`<tr data-id="${r.id}"><td>${esc(r.nombreCompleto)}</td><td>${esc(r.grupo||"—")}</td><td>${esc(r.alergias||"—")}</td><td><select class="presente" aria-label="Asistencia de ${esc(r.nombreCompleto)}" required><option value="" ${r.presente==null?"selected":""}>Sin registrar</option><option value="true" ${r.presente===true?"selected":""}>Presente</option><option value="false" ${r.presente===false?"selected":""}>Ausente</option></select></td><td><input class="obs" maxlength="500" value="${esc(r.observacion||"")}"></td></tr>`).join("")}
    </tbody></table></div><button style="margin-top:14px">Guardar asistencia</button></form>`:`<div class="empty">No hay niños asignados a tus grupos para esta fecha. Revisa el calendario de clases.</div>`}`;
    if(!$("#aForm")) return;
    $("#aForm").onsubmit=async e=>{
      e.preventDefault();
      const form=e.currentTarget;
      if(!form.reportValidity()) return;
      if($("#aFecha").value!==fecha){toast("Carga la lista de la fecha seleccionada antes de guardar.");return;}
      const registros=[...form.querySelectorAll("tbody tr")].map(tr=>({ninoId:+tr.dataset.id,presente:tr.querySelector(".presente").value==="true",observacion:tr.querySelector(".obs").value||null}));
      const button=form.querySelector("button");button.disabled=true;
      try{await api("/asistencias",{method:"POST",body:JSON.stringify({fecha,registros})});toast("Asistencia guardada");}
      catch(err){toast(err.message);}
      finally{button.disabled=false;}
    };
  };
  $("#aCargar").onclick=load;await load();
};

pages.calendarios=async()=>{
  const admin=state.user.rol===roles.admin;
  const now=new Date();
  state.calYear=state.calYear||now.getFullYear();
  state.calQuarter=state.calQuarter||trimestreActual();
  const [asigs,grupos,usuarios]=await Promise.all([
    api(`/calendarios/clases?anio=${state.calYear}`),
    api("/calendarios/grupos"),
    admin?api("/usuarios"):Promise.resolve([])
  ]);
  const mesesTri=rangoTrimestre(state.calYear,state.calQuarter).meses;
  const visibles=asigs.filter(a=>mesesTri.includes(a.mes));
  const maestras=usuarios.filter(u=>(u.rol===roles.clase||u.rol===roles.rotativa||u.rol===roles.admin)&&u.activo);
  state.gruposClase=grupos;
  const matrix=grupos.map(g=>`<tr><th>${esc(g.nombre)}<br><small>${g.edadMinima}-${g.edadMaxima} años</small></th>${mesesTri.map(m=>{
    const celda=visibles.filter(a=>a.grupoClaseId===g.id&&a.mes===m);
    return `<td>${celda.length?celda.map(a=>`<div class="teacher-pill">${esc(a.maestra)}${a.materialUrl?` <a href="${esc(a.materialUrl)}" target="_blank" title="Abrir material">📥</a>`:""}${admin?` <button class="mini-x print-hide" onclick="borrarAsignacionClase(${a.id})" title="Quitar">×</button>`:""}</div>`).join(""):`<span class="muted">Sin asignar</span>`}</td>`;
  }).join("")}</tr>`).join("");

  if(!admin){
    $("#content").innerHTML=`<div class="card print-hide"><div class="toolbar"><label>Año<input id="calAnio" type="number" min="2020" max="2100" value="${state.calYear}"></label><label>Trimestre<select id="calTri">${[1,2,3,4].map(t=>`<option value="${t}" ${t===state.calQuarter?"selected":""}>Trimestre ${t}</option>`).join("")}</select></label><button id="calVer">Ver calendario</button></div></div>
    <div class="calendar-poster"><div class="calendar-heading"><span>📅</span><div><h3>Mi calendario de clases</h3><p>${state.calYear} · Trimestre ${state.calQuarter}</p></div></div>
      ${visibles.length?`<div class="month-cards">${mesesTri.map(m=>{const items=visibles.filter(a=>a.mes===m);return `<div class="month-card"><h4>${meses[m-1]}</h4>${items.length?items.map(a=>`<div class="assignment-card"><b>${esc(a.grupo)}</b><span>${a.edadMinima}-${a.edadMaxima} años</span>${a.tema?`<p>${esc(a.tema)}</p>`:""}${a.materialUrl?`<a href="${esc(a.materialUrl)}" target="_blank">📥 Descargar / abrir material</a>`:`<span class="muted">Material pendiente</span>`}</div>`).join(""):`<div class="empty compact">Sin asignación</div>`}</div>`}).join("")}</div>`:`<div class="empty">No tienes asignaciones en este trimestre.</div>`}
    </div>`;
    $("#calVer").onclick=()=>{state.calYear=+$("#calAnio").value;state.calQuarter=+$("#calTri").value;navigate("calendarios");};
    return;
  }

  $("#content").innerHTML=`<div class="card print-hide"><div class="toolbar">
    <label>Año<input id="calAnio" type="number" min="2020" max="2100" value="${state.calYear}"></label>
    <label>Trimestre<select id="calTri">${[1,2,3,4].map(t=>`<option value="${t}" ${t===state.calQuarter?"selected":""}>Trimestre ${t}</option>`).join("")}</select></label>
    <button id="calVer">Ver</button><button class="ghost" onclick="window.print()">🖨️ Imprimir / guardar PDF</button>
  </div></div>
  <div class="calendar-poster" id="printArea"><div class="calendar-heading"><img src="/assets/logo-ninos-para-cristo.png" alt="Niños para Cristo"><div><span class="eyebrow blue">CALENDARIOS MINISTERIO NIÑOS PARA CRISTO</span><h3>División de clases ${state.calYear}</h3><p>Trimestre ${state.calQuarter} · organización por grupo, rango de edad y maestras</p></div></div>
    <div class="table-wrap calendar-matrix"><table><thead><tr><th>Grupo</th>${mesesTri.map(m=>`<th>${meses[m-1]}</th>`).join("")}</tr></thead><tbody>${matrix}</tbody></table></div>
  </div>
  <div class="card print-hide" style="margin-top:16px"><button type="button" class="ghost" onclick="navigate('importaciones')">📅 Planificar varios grupos o importar Excel</button><h3>➕ Asignar maestra a un grupo</h3><p class="muted">Si un grupo tiene dos maestras en el mes, guarda una asignación para cada una.</p><form id="calForm" class="form-grid">
    <label>Año<input id="cAnio" type="number" min="2020" max="2100" value="${state.calYear}" required></label>
    <label>Mes<select id="cMes">${meses.map((x,i)=>`<option value="${i+1}" ${i+1===mesesTri[0]?"selected":""}>${x}</option>`).join("")}</select></label>
    <label>Período<select id="cPeriodo"><option value="mes">Solo el mes elegido</option><option value="trimestre">Trimestre del mes elegido</option><option value="anio">Todo el año elegido</option></select></label>
    <label>Grupo<select id="cGrupo" required><option value="">Seleccione</option>${grupos.map(g=>`<option value="${g.id}">${esc(g.nombre)} · ${g.edadMinima}-${g.edadMaxima} años</option>`).join("")}</select></label>
    <label>Maestra<select id="cMaestra" required><option value="">Seleccione</option>${maestras.map(u=>`<option value="${u.id}">${esc(u.nombreCompleto)}</option>`).join("")}</select></label>
    <label>Tema / enfoque del mes<input id="cTema" maxlength="200"></label><label>Enlace del material<input id="cMaterial" placeholder="https://..." maxlength="1000"></label>
    <label class="full">Observaciones<textarea id="cObs" maxlength="1000"></textarea></label><div class="full"><button>Guardar asignación</button></div>
  </form></div>
  <div class="card print-hide" style="margin-top:16px"><h3>👧 Grupos y edades</h3><form id="gForm" class="form-grid mini-form">
    <label>Nombre<input id="gNombre" required maxlength="120"></label><label>Edad mínima<input id="gMin" type="number" min="0" max="18" required></label><label>Edad máxima<input id="gMax" type="number" min="0" max="18" required></label><label>Orden<input id="gOrden" type="number" min="0" value="${grupos.length+1}"></label><div class="full"><button>Agregar grupo</button></div>
  </form><div class="group-list">${grupos.map(g=>`<div class="group-chip"><b>${esc(g.nombre)}</b><span>${g.edadMinima}-${g.edadMaxima} años</span><button class="ghost btn-small" onclick="editarGrupoClase(${g.id})">Editar</button></div>`).join("")}</div></div>`;

  $("#calVer").onclick=()=>{state.calYear=+$("#calAnio").value;state.calQuarter=+$("#calTri").value;navigate("calendarios");};
  $("#calForm").onsubmit=async e=>{
    e.preventDefault();
    const form=e.currentTarget, button=form.querySelector("button");
    const anio=+$("#cAnio").value, mes=+$("#cMes").value;
    const periodo=$("#cPeriodo").value;
    const seleccion=periodo==="anio"?Array.from({length:12},(_,i)=>i+1):periodo==="trimestre"?rangoTrimestre(anio,Math.floor((mes-1)/3)+1).meses:[mes];
    const base={anio,grupoClaseId:+$("#cGrupo").value,maestraId:+$("#cMaestra").value,tema:$("#cTema").value||null,materialUrl:$("#cMaterial").value||null,observaciones:$("#cObs").value||null};
    button.disabled=true;
    try {
      const result=await api("/calendarios/clases/carga-trimestral",{method:"POST",body:JSON.stringify(seleccion.map(m=>({...base,mes:m})))});
      state.calYear=anio;state.calQuarter=Math.floor((mes-1)/3)+1;
      await navigate("calendarios");
      toast(result.agregadas+" asignaciones guardadas; "+result.omitidas+" ya existentes. Las existentes no se modifican.");
    } catch(err) {toast(err.message);}
    finally {button.disabled=false;}
  };
  $("#gForm").onsubmit=async e=>{e.preventDefault();await api("/calendarios/grupos",{method:"POST",body:JSON.stringify({nombre:$("#gNombre").value,edadMinima:+$("#gMin").value,edadMaxima:+$("#gMax").value,orden:+$("#gOrden").value})});toast("Grupo agregado");navigate("calendarios");};
};
window.borrarAsignacionClase=async id=>{if(confirm("¿Quitar esta maestra del calendario?")){await api(`/calendarios/clases/${id}`,{method:"DELETE"});navigate("calendarios");}};
window.editarGrupoClase=async id=>{const g=(state.gruposClase||[]).find(x=>x.id===id);if(!g)return;const n=prompt("Nombre del grupo",g.nombre);if(n===null)return;const mi=prompt("Edad mínima",g.edadMinima);if(mi===null)return;const ma=prompt("Edad máxima",g.edadMaxima);if(ma===null)return;await api(`/calendarios/grupos/${id}`,{method:"PUT",body:JSON.stringify({nombre:n,edadMinima:+mi,edadMaxima:+ma,orden:g.orden})});toast("Grupo actualizado");navigate("calendarios");};

pages.protocolo=async()=>{
  const admin=state.user.rol===roles.admin;
  const now=new Date();
  state.proYear=state.proYear||now.getFullYear();
  state.proQuarter=state.proQuarter||trimestreActual();
  const rango=rangoTrimestre(state.proYear,state.proQuarter);
  const [rows,usuarios]=await Promise.all([
    api(`/protocolo?desde=${rango.desde}&hasta=${rango.hasta}`),
    admin?api("/usuarios"):Promise.resolve([])
  ]);
  state.protocolRows=rows;
  const responsables=usuarios.filter(u=>(u.rol===roles.protocolo||u.rol===roles.rotativa||u.rol===roles.admin)&&u.activo);

  if(!admin){
    $("#content").innerHTML=`<div class="card print-hide"><div class="toolbar"><label>Año<input id="proAnio" type="number" value="${state.proYear}" min="2020" max="2100"></label><label>Trimestre<select id="proTri">${[1,2,3,4].map(t=>`<option value="${t}" ${t===state.proQuarter?"selected":""}>Trimestre ${t}</option>`).join("")}</select></label><button id="proVer">Ver</button></div></div>
    <div class="calendar-poster"><div class="calendar-heading"><span>🥪</span><div><h3>Mi calendario de merienda y protocolo</h3><p>${state.proYear} · Trimestre ${state.proQuarter}</p></div></div>
      ${rows.length?`<div class="protocol-cards">${rows.map(p=>`<div class="protocol-card"><div class="date-badge">${dateFmt(p.fecha)}</div>${p.responsableMeriendaId===state.user.id?`<p><b>🥪 Merienda:</b> ${esc(p.merienda||"Por definir")}</p>`:""}${p.responsableProtocoloId===state.user.id?`<p><b>🛡️ Protocolo:</b> ${esc(p.tareas||"Limpieza de pasillos y baños, recepción de la ofrenda y entrega de la merienda")}</p>`:""}<span class="badge">${p.completado?"Completado":"Pendiente"}</span><button class="ghost btn-small" onclick="bitacora(${p.id},${p.completado})">Registrar bitácora</button></div>`).join("")}</div>`:`<div class="empty">No tienes asignaciones en este trimestre.</div>`}
    </div>`;
    $("#proVer").onclick=()=>{state.proYear=+$("#proAnio").value;state.proQuarter=+$("#proTri").value;navigate("protocolo");};
    return;
  }

  $("#content").innerHTML=`<div class="card print-hide"><div class="toolbar">
    <label>Año<input id="proAnio" type="number" value="${state.proYear}" min="2020" max="2100"></label>
    <label>Trimestre<select id="proTri">${[1,2,3,4].map(t=>`<option value="${t}" ${t===state.proQuarter?"selected":""}>Trimestre ${t}</option>`).join("")}</select></label>
    <button id="proVer">Ver</button><button id="proGenerar" class="ghost">✨ Preparar domingos</button><button class="ghost" onclick="navigate('importaciones')">Planificar o importar Excel</button><button class="ghost" onclick="window.print()">🖨️ Imprimir / guardar PDF</button>
  </div></div>
  <div class="calendar-poster"><div class="calendar-heading"><img src="/assets/logo-ninos-para-cristo.png" alt="Niños para Cristo"><div><span class="eyebrow pink">CALENDARIO DE PROTOCOLO</span><h3>Meriendas y protocolo</h3><p>${state.proYear} · Trimestre ${state.proQuarter}</p></div></div>
    <div class="table-wrap"><table><thead><tr><th>Domingo</th><th>Merienda</th><th>Responsable merienda</th><th>Protocolo</th><th>Tareas</th><th>Estado</th><th class="print-hide"></th></tr></thead><tbody>
    ${rows.map(p=>`<tr><td><b>${dateFmt(p.fecha)}</b></td><td>${esc(p.merienda||"Por definir")}</td><td>${esc(p.responsableMerienda||"Sin asignar")}</td><td>${esc(p.responsableProtocolo||"Sin asignar")}</td><td>${esc(p.tareas||"Limpieza de pasillos y baños, recepción de la ofrenda y entrega de la merienda")}</td><td><span class="badge">${p.completado?"Completado":"Pendiente"}</span></td><td class="actions print-hide"><button class="ghost btn-small" onclick="cargarProtocolo(${p.id})">Editar</button><button class="ghost btn-small" onclick="bitacora(${p.id},${p.completado})">Bitácora</button><button class="ghost danger btn-small" onclick="borrarProtocolo(${p.id})">Quitar</button></td></tr>`).join("")}
    </tbody></table></div>
  </div>
  <div class="card print-hide" style="margin-top:16px"><h3 id="pFormTitle">Asignar merienda y protocolo</h3><form id="pForm" class="form-grid">
    <input id="pId" type="hidden"><label>Fecha<input id="pFecha" type="date" required></label>
    <label>Responsable merienda<select id="pMeriendaUser"><option value="">Sin asignar</option>${responsables.map(u=>`<option value="${u.id}">${esc(u.nombreCompleto)}</option>`).join("")}</select></label>
    <label>Responsable protocolo<select id="pProtocoloUser"><option value="">Sin asignar</option>${responsables.map(u=>`<option value="${u.id}">${esc(u.nombreCompleto)}</option>`).join("")}</select></label>
    <label>Merienda<input id="pMerienda" maxlength="300" placeholder="Ej. Sándwich + jugo"></label>
    <label class="full">Tareas<textarea id="pTareas" maxlength="1000">Limpieza de pasillos y baños, recepción de la ofrenda y entrega de la merienda</textarea></label>
    <div class="full actions"><button id="pGuardar">Guardar</button><button type="button" id="pCancelar" class="ghost hidden">Cancelar edición</button></div>
  </form></div>`;

  $("#proVer").onclick=()=>{state.proYear=+$("#proAnio").value;state.proQuarter=+$("#proTri").value;navigate("protocolo");};
  $("#proGenerar").onclick=async()=>{const r=await api(`/protocolo/generar-domingos?anio=${state.proYear}&trimestre=${state.proQuarter}`,{method:"POST"});toast(`${r.agregados} domingos preparados`);navigate("protocolo");};
  $("#pCancelar").onclick=()=>{state.editProtocol=null;navigate("protocolo");};
  $("#pForm").onsubmit=async e=>{e.preventDefault();const id=+$("#pId").value||null;const payload={fecha:$("#pFecha").value,responsableMeriendaId:+$("#pMeriendaUser").value||null,responsableProtocoloId:+$("#pProtocoloUser").value||null,merienda:$("#pMerienda").value||null,tareas:$("#pTareas").value||null};await api(id?`/protocolo/${id}`:"/protocolo",{method:id?"PUT":"POST",body:JSON.stringify(payload)});toast(id?"Asignación actualizada":"Asignación guardada");navigate("protocolo");};
};
window.cargarProtocolo=id=>{const p=(state.protocolRows||[]).find(x=>x.id===id);if(!p)return;$("#pId").value=p.id;$("#pFecha").value=p.fecha.substring(0,10);$("#pMeriendaUser").value=p.responsableMeriendaId||"";$("#pProtocoloUser").value=p.responsableProtocoloId||"";$("#pMerienda").value=p.merienda||"";$("#pTareas").value=p.tareas||"";$("#pFormTitle").textContent="Editar asignación";$("#pCancelar").classList.remove("hidden");$("#pForm").scrollIntoView({behavior:"smooth",block:"center"});};
window.bitacora=async(id,actual)=>{const row=(state.protocolRows||[]).find(x=>x.id===id);const text=prompt("Bitácora / observaciones realizadas",row?.bitacora||"");if(text===null)return;const ok=confirm("¿Marcar como completado?");await api(`/protocolo/${id}/bitacora`,{method:"PUT",body:JSON.stringify({bitacora:text,completado:ok})});navigate("protocolo");};
window.borrarProtocolo=async id=>{if(confirm("¿Quitar asignación?")){await api(`/protocolo/${id}`,{method:"DELETE"});navigate("protocolo");}};

pages.inventario=async()=>{
  const rows=await api("/inventario");
  $("#content").innerHTML=`<div class="card"><h3>Agregar material</h3><form id="iForm" class="form-grid">
    <label>Nombre<input id="iNombre" required></label><label>Unidad<input id="iUnidad" value="unidad" required></label>
    <label>Cantidad inicial<input id="iCantidad" type="number" min="0" value="0"></label><label>Stock mínimo<input id="iMin" type="number" min="0" value="0"></label>
    <label class="full">Descripción<input id="iDesc"></label><div class="full"><button>Guardar material</button></div>
  </form></div><div class="card" style="margin-top:16px"><h3>Inventario</h3><div class="table-wrap"><table><thead><tr><th>Material</th><th>Cantidad</th><th>Mínimo</th><th>Estado</th><th></th></tr></thead><tbody>
  ${rows.map(i=>`<tr><td>${esc(i.nombre)}<br><small>${esc(i.descripcion||"")}</small></td><td>${i.cantidad} ${esc(i.unidad)}</td><td>${i.stockMinimo}</td><td><span class="badge ${i.reponer?"warn":""}">${i.reponer?"Reponer":"Disponible"}</span></td><td class="actions"><button class="ghost btn-small" onclick="movInventario(${i.id},'Entrada')">+ Entrada</button><button class="ghost btn-small" onclick="movInventario(${i.id},'Salida')">− Salida</button></td></tr>`).join("")}</tbody></table></div></div>`;
  $("#iForm").onsubmit=async e=>{e.preventDefault();await api("/inventario",{method:"POST",body:JSON.stringify({nombre:$("#iNombre").value,descripcion:$("#iDesc").value||null,cantidad:+$("#iCantidad").value,stockMinimo:+$("#iMin").value,unidad:$("#iUnidad").value})});toast("Material agregado");navigate("inventario");};
};
window.movInventario=async(id,tipo)=>{const c=prompt(`Cantidad de ${tipo.toLowerCase()}`,"1");if(!c)return;const nota=prompt("Nota (opcional)","");await api(`/inventario/${id}/movimientos`,{method:"POST",body:JSON.stringify({tipo,cantidad:+c,nota})});navigate("inventario");};

pages.reportes=async()=>{
  const [r,c,a,aus]=await Promise.all([api("/reportes/resumen"),api(`/reportes/cumpleanos?mes=${new Date().getMonth()+1}`),api("/reportes/alergias"),api("/reportes/ausencias?semanas=3")]);
  $("#content").innerHTML=`<div class="cards">
    <div class="card"><div class="muted">👧 Niños activos</div><div class="metric">${r.totalNinos}</div></div>
    <div class="card"><div class="muted">Asistencia promedio</div><div class="metric">${r.asistenciaPromedioUltimos90Dias}%</div></div>
    <div class="card"><div class="muted">💚 Con alergias registradas</div><div class="metric">${r.ninosConAlergias}</div></div>
    <div class="card"><div class="muted">Por reponer</div><div class="metric">${r.inventarioPorReponer}</div></div>
  </div>
  <div class="card" style="margin-top:16px"><h3>Cumpleaños del mes</h3>${c.length?c.map(x=>`<p>🎂 <b>${esc(x.nombreCompleto)}</b> · ${dateFmt(x.fechaNacimiento)}</p>`).join(""):'<div class="empty">Sin cumpleaños este mes.</div>'}</div>
  <div class="card" style="margin-top:16px"><h3>Niños con alergias</h3>${a.length?a.map(x=>`<p><b>${esc(x.nombreCompleto)}</b>: ${esc(x.alergias)}</p>`).join(""):'<div class="empty">Sin alergias registradas.</div>'}</div>
  <div class="card" style="margin-top:16px"><h3>Ausencias de 3 semanas consecutivas</h3>${aus.length?aus.map(x=>`<p><b>${esc(x.nombreCompleto)}</b> · ${x.semanasAusente} semanas</p>`).join(""):'<div class="empty">No hay casos con 3 ausencias registradas consecutivas.</div>'}</div>`;
};

pages.comunicaciones=async()=>{
  const [ninos,logs]=await Promise.all([api("/ninos"),api("/comunicaciones")]);
  $("#content").innerHTML=`<div class="card"><h3>Registrar / enviar comunicación</h3><form id="cForm" class="form-grid">
    <label>Niño<select id="cNino" required><option value="">Seleccione</option>${ninos.filter(n=>n.activo).map(n=>`<option value="${n.id}">${esc(n.nombreCompleto)}</option>`).join("")}</select></label>
    <label>Canal<select id="cCanal"><option>Registro</option><option>Email</option><option>WhatsApp</option><option>Teléfono</option></select></label>
    <label class="full">Asunto<input id="cAsunto" required></label><label class="full">Mensaje<textarea id="cMensaje" required></textarea></label><div class="full"><button>Guardar comunicación</button></div>
  </form><div class="notice">El correo se envía automáticamente solo si configuras SMTP en appsettings.json. WhatsApp/Teléfono quedan registrados como gestión realizada.</div></div>
  <div class="card" style="margin-top:16px"><h3>Historial</h3><div class="table-wrap"><table><thead><tr><th>Fecha</th><th>Niño</th><th>Canal</th><th>Asunto</th><th>Estado</th></tr></thead><tbody>${logs.map(x=>`<tr><td>${new Date(x.fechaCreacion).toLocaleString()}</td><td>${esc(x.nino)}</td><td>${esc(x.canal)}</td><td>${esc(x.asunto)}</td><td>${esc(x.estado)}</td></tr>`).join("")}</tbody></table></div></div>`;
  $("#cForm").onsubmit=async e=>{e.preventDefault();await api("/comunicaciones",{method:"POST",body:JSON.stringify({ninoId:+$("#cNino").value,encargadoId:null,asunto:$("#cAsunto").value,mensaje:$("#cMensaje").value,canal:$("#cCanal").value})});toast("Comunicación registrada");navigate("comunicaciones");};
};

pages.usuarios=async()=>{
  const users=await api("/usuarios");
  $("#content").innerHTML=`<div class="card"><button class="ghost" onclick="navigate('importaciones')">Importar varias maestras desde Excel</button><h3>Crear usuario</h3><form id="uForm" class="form-grid">
    <label>Nombre completo<input id="uNombre" required></label><label>Correo<input id="uEmail" type="email" required></label>
    <label>Contraseña temporal<input id="uPass" type="password" minlength="8" required></label><label>Rol<select id="uRol"><option value="${roles.clase}">Maestra de clase</option><option value="${roles.protocolo}">Maestra de protocolo</option><option value="${roles.rotativa}">Maestra con rotación de función</option><option value="${roles.admin}">Administradora</option></select></label>
    <div class="full"><button>Crear usuario</button></div>
  </form></div><div class="card" style="margin-top:16px"><h3>Usuarios</h3><div class="table-wrap"><table><thead><tr><th>Nombre</th><th>Correo</th><th>Rol</th><th>Estado</th><th></th></tr></thead><tbody>${users.map(u=>`<tr><td>${esc(u.nombreCompleto)}</td><td>${esc(u.email)}</td><td>${esc(u.rol)}</td><td>${u.activo?"Activo":"Inactivo"}</td><td><button class="ghost btn-small" onclick="resetPass(${u.id})">Cambiar contraseña</button></td></tr>`).join("")}</tbody></table></div></div>`;
  $("#uForm").onsubmit=async e=>{e.preventDefault();await api("/usuarios",{method:"POST",body:JSON.stringify({nombreCompleto:$("#uNombre").value,email:$("#uEmail").value,password:$("#uPass").value,rol:$("#uRol").value})});toast("Usuario creado");navigate("usuarios");};
};
window.resetPass=async id=>{const p=prompt("Nueva contraseña (mínimo 8 caracteres)");if(!p)return;await api(`/usuarios/${id}/password`,{method:"PUT",body:JSON.stringify({password:p})});toast("Contraseña actualizada");};

pages.notificaciones=async()=>{
  const rows=await api("/notificaciones/mias");
  const pushSupported=("Notification" in window)&&("PushManager" in window);
  const active=pushSupported && Notification.permission==="granted" && !!(await currentPushSubscription().catch(()=>null));
  $("#content").innerHTML=`<div class="card"><h3>🔔 Notificaciones del teléfono</h3>
    <div class="push-status ${active?"ok":"warn"}"><span>${active?"✅":"📲"}</span><div><b>${active?"Notificaciones activadas":"Activa los avisos en este dispositivo"}</b><br><small>${active?"Los recordatorios pueden aparecer aunque la aplicación no esté abierta.":"Recibirás recordatorios de clase, merienda y protocolo directamente en el teléfono."}</small></div></div>
    <div class="actions"><button onclick="enablePush()">${active?"Revisar / renovar suscripción":"🔔 Activar notificaciones"}</button>${active?'<button class="ghost" onclick="testPush()">🧪 Enviar prueba</button>':""}<button class="ghost" onclick="openInstallHelp()">📲 Cómo instalar la app</button></div>
  </div>
  <div class="card" style="margin-top:16px"><h3>Historial de notificaciones</h3>${rows.length?rows.map(n=>`<div class="notice" style="${n.leida?"opacity:.65":""}"><b>${esc(n.titulo)}</b><p>${esc(n.mensaje)}</p><small>${new Date(n.fechaCreacion).toLocaleString()}</small>${!n.leida?` <button class="ghost btn-small" onclick="leerNotif(${n.id})">Marcar leída</button>`:""}</div>`).join(""):'<div class="empty">No tienes notificaciones.</div>'}</div>`;
};
window.leerNotif=async id=>{await api(`/notificaciones/${id}/leer`,{method:"PUT"});navigate("notificaciones");};

let deferredInstallPrompt = null;
let swRegistration = null;

function isStandalone(){
  return window.matchMedia("(display-mode: standalone)").matches || window.navigator.standalone === true;
}
function isIOS(){
  return /iphone|ipad|ipod/i.test(navigator.userAgent);
}
function isSecurePushContext(){
  return window.isSecureContext || location.hostname === "localhost" || location.hostname === "127.0.0.1";
}
function urlBase64ToUint8Array(base64String){
  const padding="=".repeat((4-base64String.length%4)%4);
  const base64=(base64String+padding).replace(/-/g,"+").replace(/_/g,"/");
  const raw=atob(base64);
  return Uint8Array.from([...raw].map(c=>c.charCodeAt(0)));
}
function openInstallHelp(){
  if(isStandalone()){ toast("La aplicación ya está instalada en este dispositivo."); return; }
  if(deferredInstallPrompt){
    deferredInstallPrompt.prompt();
    deferredInstallPrompt.userChoice.finally(()=>{deferredInstallPrompt=null;refreshPwaButtons();});
    return;
  }
  $("#iosInstallModal").classList.remove("hidden");
}
function closeInstallHelp(){ $("#iosInstallModal").classList.add("hidden"); }

async function ensureServiceWorker(){
  if(!("serviceWorker" in navigator)) return null;
  if(swRegistration) return swRegistration;
  try{
    swRegistration=await navigator.serviceWorker.register("/sw.js");
    await navigator.serviceWorker.ready;
    return swRegistration;
  }catch(e){ console.warn("No se pudo registrar el Service Worker",e); return null; }
}

async function currentPushSubscription(){
  const reg=await ensureServiceWorker();
  if(!reg || !("pushManager" in reg)) return null;
  return await reg.pushManager.getSubscription();
}

async function enablePush(){
  try{
    if(!state.token){ toast("Primero inicia sesión."); return; }
    if(!isSecurePushContext()){ toast("Las notificaciones requieren HTTPS."); return; }
    if(isIOS() && !isStandalone()){
      openInstallHelp();
      toast("En iPhone, instala primero la aplicación en la pantalla de inicio.");
      return;
    }
    if(!("Notification" in window) || !("PushManager" in window)){
      toast("Este navegador no admite notificaciones Push."); return;
    }
    const permission=await Notification.requestPermission();
    if(permission!=="granted"){
      toast("No se autorizó el envío de notificaciones."); refreshPwaButtons(); return;
    }
    const reg=await ensureServiceWorker();
    if(!reg) throw new Error("No se pudo preparar la aplicación para notificaciones.");
    let sub=await reg.pushManager.getSubscription();
    if(!sub){
      const key=await api("/push/public-key");
      sub=await reg.pushManager.subscribe({userVisibleOnly:true,applicationServerKey:urlBase64ToUint8Array(key.publicKey)});
    }
    const data=sub.toJSON();
    await api("/push/subscribe",{method:"POST",body:JSON.stringify({endpoint:sub.endpoint,p256dh:data.keys?.p256dh,auth:data.keys?.auth})});
    toast("🔔 Notificaciones activadas.");
    await refreshPwaButtons();
    try{
      await api("/push/test",{method:"POST"});
      toast("Te enviamos una notificación de prueba.");
    }catch(e){ console.warn(e); }
  }catch(e){ toast(e.message||"No se pudieron activar las notificaciones."); }
}

async function disablePush(){
  try{
    const sub=await currentPushSubscription();
    if(!sub){ toast("No hay notificaciones activas en este dispositivo."); return; }
    if(state.token) await api("/push/unsubscribe",{method:"POST",body:JSON.stringify({endpoint:sub.endpoint})});
    await sub.unsubscribe();
    toast("Notificaciones desactivadas en este dispositivo.");
    await refreshPwaButtons();
  }catch(e){ toast(e.message||"No se pudieron desactivar las notificaciones."); }
}

async function testPush(){
  try{
    await api("/push/test",{method:"POST"});
    toast("Notificación de prueba enviada.");
  }catch(e){ toast(e.message); }
}

async function refreshPwaButtons(){
  const installed=isStandalone();
  for(const id of ["installBtn","loginInstallBtn"]){
    const el=$("#"+id); if(el) el.classList.toggle("hidden",installed);
  }
  const pushBtn=$("#pushBtn"), testBtn=$("#pushTestBtn");
  if(!pushBtn) return;
  if(!state.token){ pushBtn.textContent="🔔 Activar notificaciones"; return; }
  try{
    const sub=await currentPushSubscription();
    const active=!!sub && Notification.permission==="granted";
    pushBtn.textContent=active?"🔕 Desactivar notificaciones":"🔔 Activar notificaciones";
    pushBtn.dataset.active=active?"1":"0";
    testBtn?.classList.toggle("hidden",!active);
  }catch{
    testBtn?.classList.add("hidden");
  }
}

window.addEventListener("beforeinstallprompt",e=>{
  e.preventDefault(); deferredInstallPrompt=e; refreshPwaButtons();
});
window.addEventListener("appinstalled",()=>{deferredInstallPrompt=null;toast("Niños para Cristo quedó instalada 💙");refreshPwaButtons();});
window.addEventListener("hashchange",()=>{
  if(!state.user) return;
  const page=location.hash.replace("#","");
  if(navItems().some(x=>x[0]===page)) navigate(page);
});

$("#installBtn")?.addEventListener("click",openInstallHelp);
$("#loginInstallBtn")?.addEventListener("click",openInstallHelp);
$("#closeInstallModal")?.addEventListener("click",closeInstallHelp);
$("#closeInstallModal2")?.addEventListener("click",closeInstallHelp);
$("#iosInstallModal")?.addEventListener("click",e=>{if(e.target.id==="iosInstallModal")closeInstallHelp();});
$("#pushBtn")?.addEventListener("click",async()=>{
  if($("#pushBtn").dataset.active==="1") await disablePush(); else await enablePush();
});
$("#pushTestBtn")?.addEventListener("click",testPush);

document.addEventListener("DOMContentLoaded",async()=>{
  await ensureServiceWorker();
  await refreshPwaButtons();
});

if(state.token && state.user) window.addEventListener("DOMContentLoaded",appStart);
