// Cargas con vista previa: tanto Excel como el planificador usan las mismas validaciones.
pages.importaciones=async()=>{
  $('#content').innerHTML=`<div class="card"><h3>Cargar varias maestras o asignaciones</h3><p>Elige cómo quieres trabajar. Primero podrás revisar los cambios; nada se guarda hasta confirmar.</p>
  <div class="toolbar"><button id="usarPlan">Planificar mes o trimestre</button><button id="usarExcel" class="ghost">Importar Excel</button></div><div id="cargaEditor"></div><div id="cargaResultado" aria-live="polite"></div></div>`;
  $('#usarExcel').onclick=excelEditor;$('#usarPlan').onclick=planEditor;await planEditor();
};
function resetPreview(){$('#cargaResultado').innerHTML='';}
async function importRequest(path,body){
  const headers={Authorization:`Bearer ${state.token}`};
  if(!(body instanceof FormData))headers['Content-Type']='application/json';
  const response=await fetch('/api/importaciones/'+path,{method:'POST',headers,body:body instanceof FormData?body:JSON.stringify(body)});
  if(response.status===401){logout();throw new Error('Sesión vencida.');}
  const result=await response.json();if(!response.ok)throw new Error(result.mensaje||'No se pudo procesar la carga.');return result;
}
async function showImportPreview(send){
  const result=await send(false);const target=$('#cargaResultado');
  const describe=r=>`<p>${r.cambios.length} ${r.guardado?'cambios guardados':'cambios por agregar'} · ${r.omitidas} registros ya existentes.</p>`;
  target.innerHTML=`<h3>Revisar antes de guardar</h3>${describe(result)}
    ${result.errores.length?`<div class="import-errors" role="alert"><b>Corrige estos errores y vuelve a revisar. No se guardó nada.</b><ul>${result.errores.map(e=>`<li>Fila ${e.fila}: ${esc(e.detalle)}</li>`).join('')}</ul></div>`:''}
    <ol class="import-preview">${result.cambios.map(c=>`<li>${esc(c.detalle)}</li>`).join('')}</ol>
    ${!result.errores.length&&result.cambios.length?'<button id="confirmarCarga">Confirmar y guardar</button>':''}`;
  target.scrollIntoView({behavior:'smooth',block:'start'});
  if(!$('#confirmarCarga'))return;
  $('#confirmarCarga').onclick=async event=>{
    const button=event.currentTarget;button.disabled=true;
    try{
      const saved=await send(true);
      if(!saved.guardado){target.innerHTML=`<div class="import-errors"><b>No se guardó la carga.</b><ul>${saved.errores.map(e=>`<li>Fila ${e.fila}: ${esc(e.detalle)}</li>`).join('')}</ul></div>`;return;}
      state.credentialsPending=saved.credenciales.length>0;
      target.innerHTML=`<div class="notice"><b>Carga completada.</b>${describe(saved)}</div>${saved.credenciales.length?`<p>Guarda las contraseñas iniciales. Entrega a cada maestra únicamente la suya.</p><button id="descargarClaves">Descargar credenciales privadas</button><div class="table-wrap"><table><thead><tr><th>Maestra</th><th>Correo</th><th>Contraseña inicial</th></tr></thead><tbody>${saved.credenciales.map(c=>`<tr><td>${esc(c.nombre)}</td><td>${esc(c.email)}</td><td>${esc(c.password)}</td></tr>`).join('')}</tbody></table></div>`:''}`;
      $('#cargaEditor').innerHTML='<p>Para otra carga, vuelve a elegir Planificar o Importar Excel.</p>';
      if($('#descargarClaves'))$('#descargarClaves').onclick=()=>{downloadBlob(new Blob([saved.credenciales.map(c=>`${c.nombre}\nCorreo: ${c.email}\nContraseña: ${c.password}`).join('\n\n')],{type:'text/plain;charset=utf-8'}),'credenciales-privadas.txt');state.credentialsPending=false;};
    }catch(error){toast(error.message);}finally{button.disabled=false;}
  };
}
function downloadBlob(blob,name){const url=URL.createObjectURL(blob);const a=document.createElement('a');a.href=url;a.download=name;a.click();setTimeout(()=>URL.revokeObjectURL(url),1000);}
function allowEditor(){if(state.credentialsPending&&!confirm('¿Ya guardaste las contraseñas? Al continuar no volverán a mostrarse.'))return false;state.credentialsPending=false;resetPreview();return true;}
async function excelEditor(){
  if(!allowEditor())return;
  $('#cargaEditor').innerHTML=`<form id="excelForm"><label>Qué quieres cargar<select id="excelTipo"><option value="maestras">Maestras</option><option value="clases">Calendario de clases</option><option value="protocolo">Meriendas y protocolo</option></select></label>
  <p>Descarga la plantilla, completa la hoja <b>Datos</b> y guarda como .xlsx. Incluye instrucciones y una hoja <b>Referencias</b> con los correos y grupos actuales para copiar. Para calendarios usa el correo de una cuenta ya creada.</p>
  <button type="button" id="plantilla" class="ghost">Descargar plantilla Excel</button>
  <label>Archivo Excel (hasta 2 MB, 500 filas)<input id="excelArchivo" type="file" accept=".xlsx" required></label><button>Revisar archivo</button></form>`;
  $('#excelTipo').onchange=()=>{$('#excelArchivo').value='';resetPreview();};$('#excelArchivo').onchange=resetPreview;
  $('#plantilla').onclick=async()=>{try{const kind=$('#excelTipo').value;const r=await fetch(`/api/importaciones/${kind}/plantilla`,{headers:{Authorization:`Bearer ${state.token}`}});if(!r.ok)throw new Error('No se pudo descargar. Revisa tu sesión.');downloadBlob(await r.blob(),`Plantilla_${kind}.xlsx`);}catch(e){toast(e.message);}};
  $('#excelForm').onsubmit=async event=>{event.preventDefault();const button=event.currentTarget.querySelector('button:last-child');button.disabled=true;try{const file=$('#excelArchivo').files[0],kind=$('#excelTipo').value;if(!file||file.size>2000000)throw new Error('Selecciona un archivo .xlsx de hasta 2 MB.');const send=confirmar=>{const data=new FormData();data.append('archivo',file);return importRequest(`${kind}/excel?confirmar=${confirmar}`,data);};await showImportPreview(send);}catch(e){toast(e.message);}finally{button.disabled=false;}};
}
async function planEditor(){
  if(!allowEditor())return;
  $('#cargaEditor').innerHTML='<p>Cargando maestras y grupos…</p>';
  try{
    const [users,groups]=await Promise.all([api('/usuarios'),api('/calendarios/grupos')]);
    const now=new Date();
    $('#cargaEditor').innerHTML=`<form id="planForm"><div class="form-grid"><label>Calendario<select id="planTipo"><option value="clases">Clases</option><option value="protocolo">Meriendas y protocolo</option></select></label>
      <label>Año<input id="planAnio" type="number" min="2020" max="2100" value="${now.getFullYear()}" required></label>
      <label>Período<select id="planPeriodo"><option value="mes">Un mes</option><option value="trimestre">Un trimestre</option></select></label>
      <label id="planMesLabel">Mes<select id="planMes">${meses.map((m,i)=>`<option value="${i+1}" ${i===now.getMonth()?'selected':''}>${m}</option>`).join('')}</select></label>
      <label id="planTriLabel" class="hidden">Trimestre<select id="planTri">${[1,2,3,4].map(t=>`<option value="${t}" ${t===trimestreActual()?'selected':''}>${meses[(t-1)*3]} – ${meses[t*3-1]}</option>`).join('')}</select></label></div>
      <button id="prepararPlan" type="button" class="ghost">Preparar período</button><div id="planFilas"></div></form>`;
    let prepared=null;
    const prepare=()=>{
      resetPreview();const year=+$('#planAnio').value;if(year<2020||year>2100){toast('Revisa el año.');return;}
      const months=$('#planPeriodo').value==='mes'?[+$('#planMes').value]:rangoTrimestre(year,+$('#planTri').value).meses;
      const kind=$('#planTipo').value;prepared={year,months,kind};
      if(kind==='clases'){
        const teachers=users.filter(u=>u.activo&&[roles.clase,roles.rotativa,roles.admin].includes(u.rol));
        $('#planFilas').innerHTML=`<p>Marca todas las maestras de cada grupo. Se aplicarán a <b>${months.map(m=>meses[m-1]).join(', ')} de ${year}</b>. Para rotaciones distintas, prepara cada mes por separado.</p>
          ${groups.map(g=>`<fieldset class="plan-group" data-grupo="${esc(g.nombre)}"><legend>${esc(g.nombre)}</legend><div class="teacher-options">${teachers.map(u=>`<label><input type="checkbox" value="${esc(u.email)}"> ${esc(u.nombreCompleto)}</label>`).join('')}</div></fieldset>`).join('')}<button>Revisar asignaciones</button>`;
      }else{
        const teachers=users.filter(u=>u.activo&&[roles.protocolo,roles.rotativa,roles.admin].includes(u.rol));
        const options='<option value="">Sin asignar</option>'+teachers.map(u=>`<option value="${esc(u.email)}">${esc(u.nombreCompleto)}</option>`).join('');
        const dates=[];for(const m of months){const last=new Date(year,m,0).getDate();for(let d=1;d<=last;d++)if(new Date(year,m-1,d).getDay()===0)dates.push(`${year}-${String(m).padStart(2,'0')}-${String(d).padStart(2,'0')}`);}
        $('#planFilas').innerHTML=`<p>Asigna las responsables para cada domingo. Las fechas que dejes vacías se omiten. Los turnos existentes con datos diferentes se conservan y se informa el conflicto.</p>${dates.map(d=>`<fieldset class="plan-group" data-fecha="${d}"><legend>Domingo ${dateFmt(d)}</legend><div class="form-grid"><label>Protocolo<select class="plan-pro">${options}</select></label><label>Merienda<select class="plan-mer">${options}</select></label></div></fieldset>`).join('')}<button>Revisar asignaciones</button>`;
      }
      $('#planFilas').onchange=resetPreview;
    };
    for(const id of ['planTipo','planAnio','planPeriodo','planMes','planTri'])$('#'+id).onchange=()=>{prepared=null;$('#planFilas').innerHTML='';resetPreview();const quarter=$('#planPeriodo').value==='trimestre';$('#planMesLabel').classList.toggle('hidden',quarter);$('#planTriLabel').classList.toggle('hidden',!quarter);};
    $('#prepararPlan').onclick=prepare;prepare();
    $('#planForm').onsubmit=async event=>{event.preventDefault();if(!prepared)return;const button=event.currentTarget.querySelector('#planFilas button');button.disabled=true;try{
      const {year,months,kind}=prepared;let rows=[];
      if(kind==='clases')for(const group of document.querySelectorAll('#planFilas fieldset'))for(const box of group.querySelectorAll('input:checked'))rows.push({fila:rows.length+1,valores:{Anio:String(year),MesInicio:String(months[0]),MesFin:String(months.at(-1)),Grupo:group.dataset.grupo,Correo:box.value,Tema:''}});
      else for(const group of document.querySelectorAll('#planFilas fieldset')){const p=group.querySelector('.plan-pro').value,s=group.querySelector('.plan-mer').value;if(p||s)rows.push({fila:rows.length+1,valores:{Fecha:group.dataset.fecha,CorreoProtocolo:p,CorreoMerienda:s,Merienda:'',Tareas:''}});}
      if(!rows.length)throw new Error('Selecciona al menos una maestra.');await showImportPreview(confirmar=>importRequest(`${kind}/lote?confirmar=${confirmar}`,rows));
    }catch(e){toast(e.message);}finally{button.disabled=false;}};
  }catch(e){$('#cargaEditor').textContent=e.message;}
}
